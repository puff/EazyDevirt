using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDevirtualizer : Stage
{
    private CryptoStreamV3 VMStream { get; set; }
    private VMBinaryReader VMStreamReader { get; set; }
    
    private Resolver Resolver { get; set; }
    
    public override bool Run()
    {
        if (!Init()) return false;
        
        VMStream = new CryptoStreamV3(Ctx.VMStream, Ctx.MethodCryptoKey, true);
        VMStreamReader = new VMBinaryReader(VMStream);
        
        Resolver = new Resolver(Ctx);
        foreach (var vmMethod in Ctx.VMMethods)
        { 
            // if (vmMethod.EncodedMethodKey != @"5<]fEBf\76") continue;
            // if (vmMethod.EncodedMethodKey != @"5<_4mf/boO") continue;
            
            vmMethod.MethodKey = VMCipherStream.DecodeMethodKey(vmMethod.EncodedMethodKey, Ctx.PositionCryptoKey);
            
            VMStream.Seek(vmMethod.MethodKey, SeekOrigin.Begin);

            ReadVMMethod(vmMethod);
            
            if (Ctx.Options.VeryVerbose)
                Ctx.Console.Info(vmMethod);
        }
        
        VMStreamReader.Dispose();
        return true;
    }
    
    private void ReadVMMethod(VMMethod vmMethod)
    {
        vmMethod.MethodInfo = new VMMethodInfo(VMStreamReader);

        ReadExceptionHandlers(vmMethod);
        
        vmMethod.MethodInfo.DeclaringType = Resolver.ResolveType(vmMethod.MethodInfo.VMDeclaringType)!;
        vmMethod.MethodInfo.ReturnType = Resolver.ResolveType(vmMethod.MethodInfo.VMReturnType)!;
        
        ResolveLocalsAndParameters(vmMethod);

        if (!ReadInstructions(vmMethod) && (!Ctx.Options.SaveAnyway || Ctx.Options.OnlySaveDevirted))
            return;

        ResolveBranchTargets(vmMethod);

        ResolveExceptionHandlers(vmMethod);

        vmMethod.Parent.CilMethodBody!.LocalVariables.Clear();
        vmMethod.Locals.ForEach(x => vmMethod.Parent.CilMethodBody.LocalVariables.Add(x));

        // vmMethod.Parent.CilMethodBody!.ExceptionHandlers.Clear();
        // vmMethod.ExceptionHandlers.ForEach(x => vmMethod.Parent.CilMethodBody.ExceptionHandlers.Add(x));

        // TODO: Remove this when all opcodes are properly handled
        vmMethod.Parent.CilMethodBody!.VerifyLabelsOnBuild = false;
        vmMethod.Parent.CilMethodBody!.ComputeMaxStackOnBuild = false;

        vmMethod.Parent.CilMethodBody.Instructions.Clear();
        vmMethod.Instructions.ForEach(x => vmMethod.Parent.CilMethodBody.Instructions.Add(x));
        // vmMethod.Parent.CilMethodBody.Instructions.CalculateOffsets();
    }
    
    private void ReadExceptionHandlers(VMMethod vmMethod)
    {
        vmMethod.VMExceptionHandlers = new List<VMExceptionHandler>(VMStreamReader.ReadInt16());
        for (var i = 0; i < vmMethod.VMExceptionHandlers.Capacity; i++)
            vmMethod.VMExceptionHandlers.Add(new VMExceptionHandler(VMStreamReader));

        vmMethod.VMExceptionHandlers.Sort((first, second) =>
            first.TryStart == second.TryStart
                ? second.TryLength.CompareTo(first.TryLength)
                : first.TryStart.CompareTo(second.TryStart));
    }
    
    private void ResolveLocalsAndParameters(VMMethod vmMethod)
    {
        vmMethod.Locals = new List<CilLocalVariable>();
        foreach (var local in vmMethod.MethodInfo.VMLocals)
        {
            var type = Resolver.ResolveType(local.VMType)!;
            var res = type.Resolve();
            vmMethod.Locals.Add(new CilLocalVariable(type.ToTypeSignature((res?.IsValueType).GetValueOrDefault())));

            // if (Ctx.Options.VeryVeryVerbose)
            //     Ctx.Console.Info($"[{vmMethod.MethodInfo.Name}] Local: {local.Type.Name}");
        }
        
        // the parameters should already be the correct types and in the correct order so we don't need to resolve those
    }

    private bool ReadInstructions(VMMethod vmMethod)
    {
        vmMethod.Instructions = new List<CilInstruction>();
        vmMethod.CodeSize = VMStreamReader.ReadInt32();
        vmMethod.InitialCodeStreamPosition = VMStream.Position;
        var finalPosition = VMStream.Position + vmMethod.CodeSize;
        var success = true;
        
        while (VMStream.Position < finalPosition)
        {
            vmMethod.CodePosition = vmMethod.CodeSize - (finalPosition - VMStream.Position);
            var virtualOpCode = VMStreamReader.ReadInt32Special();
            var vmOpCode = Ctx.PatternMatcher.GetOpCodeValue(virtualOpCode);
            if (!vmOpCode.HasVirtualCode)
            {
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] Instruction {vmMethod.Instructions.Count}, VM opcode [{virtualOpCode}] not found!");
                
                vmMethod.Instructions.Add(new CilInstruction(CilOpCodes.Nop));
                continue;
            }

            if (!vmOpCode.IsIdentified)
            {
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.Warning($"[{vmMethod.Parent.MetadataToken}] Instruction {vmMethod.Instructions.Count} vm opcode not identified [{vmOpCode}]");

                success = false;
            }

            var operand = vmOpCode.IsSpecial ? ReadSpecialOperand(vmOpCode, vmMethod) : ReadOperand(vmOpCode, vmMethod);
            if (vmOpCode.CilOpCode != null)
            {
                // TODO: Remember to remove the log for stinds
                // Log these for now since they're special cases. 
                if (vmOpCode.CilOpCode.Value.Mnemonic.StartsWith("stind"))
                    Ctx.Console.Warning($"Placing stind instruction at #{vmMethod.Instructions.Count}");


                var instruction =
                    new CilInstruction(vmOpCode.CilOpCode.Value,
                        vmOpCode.IsIdentified ? operand : operand); // TODO: remember to switch the alternate to null
                vmMethod.Instructions.Add(instruction);
            }
        }

        if (vmMethod.HasHomomorphicEncryption)
            success = false;
        
        return success;
    }

    private Dictionary<int, int> GetVirtualOffsets(VMMethod vmMethod)
    {
        var virtualOffsets = new Dictionary<int, int>(vmMethod.Instructions.Count);
        var lastCilOffset = 0;
        var lastOffset = 0;
        foreach (var ins in vmMethod.Instructions)
        {
            if (ins.OpCode == CilOpCodes.Switch)
            {
                var offsetsLength = (ins.Operand as uint[])!.Length;
                lastOffset += 4 * offsetsLength + 8;
                lastCilOffset += ins.OpCode.Size + 4 + 4 * offsetsLength;
            }
            else
            {
                lastOffset += ins.OpCode.OperandType == CilOperandType.ShortInlineBrTarget
                    ? 8
                    : ins.Size - ins.OpCode.Size + 4;
                lastCilOffset += ins.Size;
            }

            virtualOffsets.Add(lastOffset, lastCilOffset);
        }

        return virtualOffsets;
    }
    
    private void ResolveBranchTargets(VMMethod vmMethod)
    {
        var virtualOffsets = GetVirtualOffsets(vmMethod);

        for (var i = 0; i < vmMethod.Instructions.Count; i++)
        {
            var ins = vmMethod.Instructions[i];
            ins.Offset = virtualOffsets[virtualOffsets.Keys.ToArray()[i]];
            switch (ins.OpCode.OperandType)
            {
                case CilOperandType.InlineBrTarget:
                case CilOperandType.ShortInlineBrTarget:
                    ins.Operand = new CilOffsetLabel(virtualOffsets[(int)(uint)ins.Operand!]);
                    break;
                case CilOperandType.InlineSwitch:
                    var offsets = ins.Operand as uint[];
                    var labels = new ICilLabel[offsets!.Length];
                    for (var x = 0; x < offsets.Length; x++)
                        labels[x] = new CilOffsetLabel(virtualOffsets[(int)offsets[x]]);
                    ins.Operand = labels;
                    break;
            }
        }
    }

    private void ResolveExceptionHandlers(VMMethod vmMethod)
    {
        vmMethod.ExceptionHandlers = new List<CilExceptionHandler>();
        
        var virtualOffsets = GetVirtualOffsets(vmMethod);
        var virtualOffsetsValues = virtualOffsets.Values.ToList();
        foreach (var vmExceptionHandler in vmMethod.VMExceptionHandlers)
        {
            var tryStart = vmMethod.Instructions[virtualOffsetsValues.IndexOf((int)vmExceptionHandler.TryStart)];
            // var tryStart = vmMethod.Instructions.GetByOffset(virtualOffsets[(int)vmExceptionHandler.TryStart]);
            var tryStartLabel = vmMethod.Instructions.SkipWhile(x => x.Offset <= tryStart?.Offset).First().CreateLabel();

            var handlerStart = vmMethod.Instructions[virtualOffsetsValues.IndexOf((int)vmExceptionHandler.HandlerStart)];
            // var handlerStart = vmMethod.Instructions.GetByOffset(virtualOffsets[(int)vmExceptionHandler.HandlerStart]);
            var handlerStartLabel = vmMethod.Instructions.SkipWhile(x => x.Offset <= handlerStart?.Offset).First().CreateLabel();
            var exceptionHandler = new CilExceptionHandler
            {
                ExceptionType = vmExceptionHandler.HandlerType == CilExceptionHandlerType.Exception ? Resolver.ResolveType(vmExceptionHandler.CatchType) : null,
                HandlerType = vmExceptionHandler.HandlerType,
                TryStart = tryStartLabel,
                TryEnd = handlerStartLabel,
                HandlerStart = handlerStartLabel,
                HandlerEnd = handlerStart?.Operand as ICilLabel,
                FilterStart = vmExceptionHandler.HandlerType == CilExceptionHandlerType.Filter ? vmMethod.Instructions.GetByOffset(virtualOffsets[(int)vmExceptionHandler.FilterStart])?.CreateLabel() : new CilOffsetLabel(0),
            };
            
            vmMethod.Parent.CilMethodBody?.ExceptionHandlers.Add(exceptionHandler);
        }
        
    }

    private object? ReadOperand(VMOpCode vmOpCode, VMMethod vmMethod) =>
        vmOpCode.CilOperandType switch // maybe switch this to vmOpCode.CilOpCode.OperandType and add more handlers
        {
            CilOperandType.InlineI => VMStreamReader.ReadInt32Special(),
            CilOperandType.ShortInlineI => VMStreamReader.ReadSByte(),
            CilOperandType.InlineI8 => VMStreamReader.ReadInt64(),
            CilOperandType.InlineR => VMStreamReader.ReadDouble(),
            CilOperandType.ShortInlineR => VMStreamReader.ReadSingle(),
            CilOperandType.InlineVar => IsInlineArgument(vmOpCode.CilOpCode) ? GetArgument(vmMethod, VMStreamReader.ReadUInt16()) : GetLocal(vmMethod, VMStreamReader.ReadUInt16()),
            CilOperandType.ShortInlineVar => IsInlineArgument(vmOpCode.CilOpCode) ? GetArgument(vmMethod, VMStreamReader.ReadByte()) : GetLocal(vmMethod, VMStreamReader.ReadByte()),
            CilOperandType.InlineTok => ReadInlineTok(vmOpCode),
            CilOperandType.InlineSwitch => ReadInlineSwitch(),
            CilOperandType.InlineBrTarget => VMStreamReader.ReadUInt32(),
            CilOperandType.InlineArgument => GetArgument(vmMethod, VMStreamReader.ReadUInt16()),    // this doesn't seem to be used, might not be correct
            CilOperandType.ShortInlineArgument => GetArgument(vmMethod, VMStreamReader.ReadByte()), // this doesn't seem to be used, might not be correct
            CilOperandType.InlineNone => null,
            _ => null
        };

    private object? ReadSpecialOperand(VMOpCode vmOpCode, VMMethod vmMethod) =>
        vmOpCode.SpecialOpCode switch
        {
            SpecialOpCodes.EazCall => Resolver.ResolveEazCall(VMStreamReader.ReadInt32Special()),
            SpecialOpCodes.StartHomomorphic => ReadHomomorphicEncryption(vmMethod),
            _ => null
        };

    /// <summary>
    /// Processes homomorphic encryption data into CIL instructions 
    /// </summary>
    /// <param name="method"></param>
    /// <returns>
    /// branch offset
    /// </returns>
    private int? ReadHomomorphicEncryption(VMMethod vmMethod)
    {
        Ctx.Console.Info($"[{vmMethod.Parent.MetadataToken}] Detected homomorphic encryption.");

        vmMethod.HasHomomorphicEncryption = true;
        return null;
    }

    private object? ReadInlineTok(VMOpCode vmOpCode) =>
        vmOpCode.CilOpCode?.OperandType switch
        {
            CilOperandType.InlineString => Resolver.ResolveString(VMStreamReader.ReadInt32Special()),
            _ => Resolver.ResolveToken(VMStreamReader.ReadInt32Special())
        };

    private int[] ReadInlineSwitch()
    {
        var destCount = VMStreamReader.ReadInt32Special();
        var branchDests = new int[destCount];
        for (var i = 0; i < destCount; i++)
            branchDests[i] = VMStreamReader.ReadInt32Special();
        return branchDests;
    }

    // private static void ResolveSpecialCilOpCode(VMOpCode vmOpCode) =>
    //     vmOpCode.CilOpCode = vmOpCode.SpecialOpCode switch
    //     {
    //         SpecialOpCodes.EazCall => CilOpCodes.Call,
    //         _ => vmOpCode.CilOpCode
    //     };
    
    private static Parameter GetArgument(VMMethod vmMethod, int index) => (index < vmMethod.Parent.Parameters.Count ? vmMethod.Parent.Parameters[index] : null)!;
    // private static TypeSignature GetArgument(VMMethod vmMethod, int index) => (index < vmMethod.Parameters.Count ? vmMethod.Parameters[index] : null)!;

    private static CilLocalVariable GetLocal(VMMethod vmMethod, int index) => (index < vmMethod.Locals.Count ? vmMethod.Locals[index] : null)!;

    private static bool IsInlineArgument(CilOpCode? opCode) => opCode?.OperandType is CilOperandType.InlineArgument or CilOperandType.ShortInlineArgument;

#pragma warning disable CS8618
    public MethodDevirtualizer(DevirtualizationContext ctx) : base(ctx)
    {
    }
#pragma warning restore CS8618
}