using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;
using EazyDevirt.PatternMatching.Patterns.OpCodes;

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

        // var codePosition = VMStream.Position;
        
        vmMethod.MethodInfo.DeclaringType = Resolver.ResolveType(vmMethod.MethodInfo.VMDeclaringType);
        vmMethod.MethodInfo.ReturnType = Resolver.ResolveType(vmMethod.MethodInfo.VMReturnType);

        // TODO: may need to add SortVMExceptionHandlers
        
        ResolveLocalsAndParameters(vmMethod);
        
        if (Ctx.Options.VeryVeryVerbose)
            Ctx.Console.Info(vmMethod);
        
        // VMStream.Seek(codePosition, SeekOrigin.Begin);
        ReadInstructions(vmMethod);
    }
    
    private void ResolveLocalsAndParameters(VMMethod vmMethod)
    {
        foreach (var local in vmMethod.MethodInfo.VMLocals)
        {
            local.Type = Resolver.ResolveType(local.VMType);
         
            // if (Ctx.Options.VeryVeryVerbose)
            //     Ctx.Console.Info($"[{vmMethod.MethodInfo.Name}] Local: {local.Type.Name}");
        }
        
        foreach (var parameter in vmMethod.MethodInfo.VMParameters)
        {
            parameter.Type = Resolver.ResolveType(parameter.VMType);
            
            // if (Ctx.Options.VeryVeryVerbose)
            //     Ctx.Console.Info($"[{vmMethod.MethodInfo.Name}] Parameter: {parameter.Type.Name}");
        }
    }
    
    private void ReadInstructions(VMMethod vmMethod)
    {
        var codeSize = VMStreamReader.ReadInt32();
        
        var finalPosition = VMStream.Position + codeSize;
        while (VMStream.Position < finalPosition)
        {
            var virtualOpCode = VMStreamReader.ReadInt32Special();
            var vmOpCode = Ctx.PatternMatcher.GetOpCodeValue(virtualOpCode);
            if (!vmOpCode.HasVirtualCode)
            {
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.Error($"Method {vmMethod.Parent} {vmMethod.EncodedMethodKey}, VM opcode [{vmOpCode}] not identified!");
                
                break;
            }

            var operand = ReadOperand(vmOpCode, vmMethod);

            // TODO: When adding resolved instructions, make sure to set cil opcode to Nop and operand to null if !vmOpCode.IsIdentified

            break; // This is only here because not all operand types have been handled yet, so the stream position won't be set properly
        }
    }

    private object? ReadOperand(VMOpCode vmOpCode, VMMethod vmMethod) =>
        vmOpCode.CilOperandType switch
        {
            CilOperandType.InlineI => VMStreamReader.ReadInt32Special(),
            CilOperandType.ShortInlineI => VMStreamReader.ReadSByte(),
            CilOperandType.InlineI8 => VMStreamReader.ReadInt64(),
            CilOperandType.InlineR => VMStreamReader.ReadDouble(),
            CilOperandType.ShortInlineR => VMStreamReader.ReadSingle(),
            CilOperandType.InlineVar => IsInlineArgument(vmOpCode.CilOpCode) ? GetArgument(vmMethod, VMStreamReader.ReadUInt16()) : GetLocal(vmMethod, VMStreamReader.ReadUInt16()),
            CilOperandType.ShortInlineVar => IsInlineArgument(vmOpCode.CilOpCode) ? GetArgument(vmMethod, VMStreamReader.ReadByte()) : GetLocal(vmMethod, VMStreamReader.ReadByte()),
            CilOperandType.InlineTok => ResolveInlineTok(vmOpCode),
            
            _ => null
        };

    private object? ResolveInlineTok(VMOpCode vmOpCode) =>
        vmOpCode.CilOpCode.OperandType switch
        {
            CilOperandType.InlineString => Resolver.ResolveString(VMStreamReader.ReadInt32Special()),
            
            _ => Resolver.ResolveToken(VMStreamReader.ReadInt32Special())
        };

    private static ITypeDefOrRef GetArgument(VMMethod vmMethod, int index) => (index < vmMethod.MethodInfo.VMParameters.Count ? vmMethod.MethodInfo.VMParameters[index].Type : null)!;

    private static ITypeDefOrRef GetLocal(VMMethod vmMethod, int index) => (index < vmMethod.MethodInfo.VMLocals.Count ? vmMethod.MethodInfo.VMLocals[index].Type : null)!;

    private static bool IsInlineArgument(CilOpCode opCode) => opCode.OperandType is CilOperandType.InlineArgument or CilOperandType.ShortInlineArgument;

#pragma warning disable CS8618
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
#pragma warning restore CS8618
}