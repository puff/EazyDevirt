using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDisassembler : Stage
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
        }
        
        VMStreamReader.Dispose();
        return false;
    }
    
    private void ReadVMMethod(VMMethod vmMethod)
    {
        vmMethod.MethodInfo = new VMMethodInfo(VMStreamReader);
        
        vmMethod.VMExceptionHandlers = new List<VMExceptionHandler>(VMStreamReader.ReadInt16());
        for (var i = 0; i < vmMethod.VMExceptionHandlers.Capacity; i++)
            vmMethod.VMExceptionHandlers.Add(new VMExceptionHandler(VMStreamReader));
        
        vmMethod.MethodInfo.DeclaringType = Resolver.ResolveType(vmMethod.MethodInfo.VMDeclaringType)!;
        vmMethod.MethodInfo.ReturnType = Resolver.ResolveType(vmMethod.MethodInfo.VMReturnType)!;

        // TODO: may need to add SortVMExceptionHandlers
        
        ResolveLocalsAndParameters(vmMethod);
        ReadInstructions(vmMethod);
        
        if (Ctx.Options.VeryVeryVerbose)
            Ctx.Console.Info(vmMethod);
    }
    
    private void ResolveLocalsAndParameters(VMMethod vmMethod)
    {
        vmMethod.Parent.CilMethodBody!.LocalVariables.Clear();
        foreach (var local in vmMethod.MethodInfo.VMLocals)
        {
            var type = Resolver.ResolveType(local.VMType)!;
            // local.Type = Resolver.ResolveType(local.VMType)!;
            vmMethod.Parent.CilMethodBody!.LocalVariables.Add(new CilLocalVariable(type));

            // if (Ctx.Options.VeryVeryVerbose)
            //     Ctx.Console.Info($"[{vmMethod.MethodInfo.Name}] Local: {local.Type.Name}");
        }
        
        // hopefully the parameters are already in the correct order so we don't need to resolve those
    }
    
    private void ReadInstructions(VMMethod vmMethod)
    {
        vmMethod.Instructions = new CilInstructionCollection(vmMethod.Parent.CilMethodBody!);
        
        var codeSize = VMStreamReader.ReadInt32();
        
        var finalPosition = VMStream.Position + codeSize;
        while (VMStream.Position < finalPosition)
        {
            var virtualOpCode = VMStreamReader.ReadInt32Special();
            var vmOpCode = Ctx.PatternMatcher.GetOpCodeValue(virtualOpCode);
            if (!vmOpCode.HasVirtualCode)
            {
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.Error($"Method {vmMethod.Parent} {vmMethod.EncodedMethodKey}, VM opcode [{vmOpCode}] not found!");
                break;
            }

            object? operand;
            if (vmOpCode.IsSpecial)
            {
                // ResolveSpecialCilOpCode(vmOpCode);
                operand = ReadSpecialOperand(vmOpCode, vmMethod);
            }
            else
                operand = ReadOperand(vmOpCode, vmMethod);

            if (!vmOpCode.IsIdentified && Ctx.Options.VeryVerbose)
                Ctx.Console.Warning($"Instruction {vmMethod.Instructions.Count} vm opcode not identified [{vmOpCode}]");
            
            var instruction = new CilInstruction(vmOpCode.CilOpCode, vmOpCode.IsIdentified ? operand : operand); // TODO: remember to switch the alternate to null
            vmMethod.Instructions.Add(instruction);

            // break; // This is only here because not all operand types have been handled yet, so the stream position won't be set properly
        }

        // vmMethod.Parent.CilMethodBody!.VerifyLabelsOnBuild = false;
        // vmMethod.Parent.CilMethodBody!.ComputeMaxStackOnBuild = false;
        // vmMethod.Parent.CilMethodBody.Instructions.Clear();
        // vmMethod.Instructions.ToList().ForEach(x => vmMethod.Parent.CilMethodBody.Instructions.Add(x));
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

    private object? ReadSpecialOperand(VMOpCode vmOpCode, VMMethod method) =>
        vmOpCode.SpecialOpCode switch
        {
            SpecialOpCodes.EazCall => Resolver.ResolveEazCall(VMStreamReader.ReadInt32Special()),
            _ => null
        };

    private object? ReadInlineTok(VMOpCode vmOpCode) =>
        vmOpCode.CilOpCode.OperandType switch
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
    // private static TypeSignature GetArgument(VMMethod vmMethod, int index) => (index < vmMethod.MethodInfo.VMParameters.Count ? vmMethod.MethodInfo.VMParameters[index].Type : null)!;

    private static CilLocalVariable GetLocal(VMMethod vmMethod, int index) => (index < vmMethod.Parent.CilMethodBody!.LocalVariables.Count ? vmMethod.Parent.CilMethodBody.LocalVariables[index] : null)!;
    // private static TypeSignature GetLocal(VMMethod vmMethod, int index) => (index < vmMethod.MethodInfo.VMLocals.Count ? vmMethod.MethodInfo.VMLocals[index].Type : null)!;

    private static bool IsInlineArgument(CilOpCode opCode) => opCode.OperandType is CilOperandType.InlineArgument or CilOperandType.ShortInlineArgument;

#pragma warning disable CS8618
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
#pragma warning restore CS8618
}