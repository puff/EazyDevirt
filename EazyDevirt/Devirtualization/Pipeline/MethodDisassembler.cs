using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;
using Org.BouncyCastle.Crypto.Digests;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDisassembler : Stage
{
    private CryptoStreamV3 VMStream { get; set; }
    private VMBinaryReader VMStreamReader { get; set; }
    
    private Resolver Resolver { get; set; }
    
    // private VMOpCode PreviousReadVMOpCode { get; set; }
    
    public override bool Run()
    {
        if (!Init()) return false;
        
        VMStream = new CryptoStreamV3(Ctx.VMStream, Ctx.MethodCryptoKey, true);
        VMStreamReader = new VMBinaryReader(VMStream, true);
        Resolver = new Resolver(Ctx);
        foreach (var vmMethod in Ctx.VMMethods)
        { 
            // if (vmMethod.EncodedMethodKey != @"5<]fEBf\76") continue;
            // if (vmMethod.EncodedMethodKey != @"5<_4mf/boO") continue;
            
            vmMethod.MethodKey = VMCipherStream.DecodeMethodKey(vmMethod.EncodedMethodKey, Ctx.PositionCryptoKey);
            
            VMStream.Seek(vmMethod.MethodKey, SeekOrigin.Begin);
            
            ReadVMMethod(vmMethod);
        }
        
        VMStream.Dispose();
        VMStreamReader.Dispose();
        return false;
    }
    
    private void ReadVMMethod(VMMethod vmMethod)
    {
        vmMethod.MethodInfo = new VMMethodInfo(VMStreamReader);
        
        vmMethod.VMExceptionHandlers = new List<VMExceptionHandler>(VMStreamReader.ReadInt16());
        for (var i = 0; i < vmMethod.VMExceptionHandlers.Capacity; i++)
            vmMethod.VMExceptionHandlers.Add(new VMExceptionHandler(VMStreamReader));
        
        vmMethod.MethodInfo.DeclaringType = Resolver.ResolveType(vmMethod.MethodInfo.VMDeclaringType);
        vmMethod.MethodInfo.ReturnType = Resolver.ResolveType(vmMethod.MethodInfo.VMReturnType);

        // TODO: may need to add SortVMExceptionHandlers
        
        ResolveLocalsAndParameters(vmMethod);
        
        if (Ctx.Options.VeryVeryVerbose)
            Ctx.Console.Info(vmMethod);
        
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
            // TODO: opcode matching
            var virtualOpCode = VMStreamReader.ReadInt32();
            var vmOpCode = Ctx.PatternMatcher.GetOpCodeValue(virtualOpCode);
            // PreviousReadVMOpCode = vmOpCode;
            break;
        }
    }
    
#pragma warning disable CS8618
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
#pragma warning restore CS8618
}