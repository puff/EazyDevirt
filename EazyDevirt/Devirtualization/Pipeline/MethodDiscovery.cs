using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;

namespace EazyDevirt.Devirtualization.Pipeline;

internal sealed class MethodDiscovery : Stage
{
    private protected override bool Init()
    {
        foreach (var t in Ctx.Module.GetAllTypes())
        {
            if (Ctx.MethodCryptoKey != 0) break;
            foreach (var m in t.Methods)
            {
                if (!IsLoadVMPositionMethod(m)) continue;
                
                // The instruction indices are the same across all samples I've analyzed.
                var getVMMethodCryptoKeyMethod = (SerializedMethodDefinition)m.CilMethodBody!.Instructions[1].Operand!;
                if (!IsCryptoKeyMethod(getVMMethodCryptoKeyMethod))
                    continue;
                
                Ctx.MethodCryptoKey = getVMMethodCryptoKeyMethod.CilMethodBody!.Instructions[0].GetLdcI4Constant();
                if (Ctx.Options.Verbose)
                {
                    Ctx.Console.Success("Found VM method crypto key!");
                    if (Ctx.Options.VeryVerbose)
                        Ctx.Console.InfoStr("VM Method Crypto Key", Ctx.MethodCryptoKey);
                }

                // this should be in the same method
                var decryptVMPositionMethod = (SerializedMethodDefinition)m.CilMethodBody!.Instructions[15].Operand!;
                if (!IsDecryptPositionMethod(decryptVMPositionMethod))
                {
                    Ctx.Console.Error("Failed to find VM position decrypt method.");
                    return false;
                }

                var getVMPositionCryptoKeyMethod = (SerializedMethodDefinition)decryptVMPositionMethod.CilMethodBody!.Instructions[6].Operand!;
                if (!IsCryptoKeyMethod(getVMPositionCryptoKeyMethod))
                {
                    Ctx.Console.Error("Failed to find VM position crypto key.");
                    return false;
                }

                Ctx.PositionCryptoKey = getVMPositionCryptoKeyMethod.CilMethodBody!.Instructions[0].GetLdcI4Constant();
                if (Ctx.Options.Verbose)
                {
                    Ctx.Console.Success("Found VM position crypto key!");
                    if (Ctx.Options.VeryVerbose)
                        Ctx.Console.InfoStr("VM Position Crypto Key", Ctx.PositionCryptoKey);
                }

                break;
            }
        }

        if (Ctx.MethodCryptoKey == 0)
        {
            Ctx.Console.Error("Failed to find vm method crypto key.");
            return false;
        }

        Ctx.VMMethods = new List<VMMethod>();

        return true;
    }

    public override bool Run()
    {
        if (!Init()) return false;

        foreach (var t in Ctx.Module.GetAllTypes())
        {
            foreach (var m in t.Methods)
            {
                if (m.CilMethodBody == null) continue;

                var instructions = m.CilMethodBody.Instructions;
                var index = -1;
                for (var i = 0; i < instructions.Count; i++)
                {
                    var ins = instructions[i];
                    if (ins.OpCode != CilOpCodes.Call
                        || ins.Operand!.GetType() != typeof(SerializedMethodDefinition)
                        || ((SerializedMethodDefinition)ins.Operand).MetadataToken != Ctx.VMResourceGetterMdToken)
                        continue;
                    index = i;
                    break;
                }
                if (index == -1)
                    continue;

                if (instructions[index + 1].OpCode != CilOpCodes.Ldstr)
                {
                    if (Ctx.Options.Verbose)
                        Ctx.Console.Error($"Expected ldstr on instruction {index + 1} for method {m.MetadataToken}");
                    
                    continue;
                }
                
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.InfoStr("Virtualized method found", m.MetadataToken);
                
                Ctx.VMMethods.Add(new VMMethod(m, (string)instructions[index + 1].Operand!));
            }
        }

        if (Ctx.Options.Verbose)
            Ctx.Console.Success($"Discovered {Ctx.VMMethods.Count} virtualized methods!");

        return true;
    }

    private static bool IsDecryptPositionMethod(MethodDefinition method) =>
        method.Signature?.ReturnType.FullName == typeof(long).FullName
        && method.Parameters.Count == 1
        && method.Parameters[0].ParameterType.FullName == typeof(string).FullName
        && method.CilMethodBody != null
        && method.CilMethodBody.Instructions[6].OpCode == CilOpCodes.Call; // 6	000E	call	instance int32 VM::GetVMPositionCryptoKey()

    private static bool IsCryptoKeyMethod(MethodDefinition method) =>
        method.Signature?.ReturnType.FullName == typeof(int).FullName
        && method.CilMethodBody != null
        && method.CilMethodBody.Instructions.Count == 2
        && method.CilMethodBody.Instructions[0].IsLdcI4();

    private static bool IsLoadVMPositionMethod(MethodDefinition method) =>
        !method.IsStatic
        && method.Signature is { ReturnsValue: false }
        && method.Parameters.Count == 3
        && method.Parameters[0].ParameterType.FullName == typeof(Stream).FullName
        && method.Parameters[1].ParameterType.FullName == typeof(long).FullName
        && method.Parameters[2].ParameterType.FullName == typeof(string).FullName
        && method.CilMethodBody != null
        && method.CilMethodBody.Instructions[1].OpCode == CilOpCodes.Call // 1	0001	call	instance int32 VM::GetVMMethodCryptoKey()
        && method.CilMethodBody.Instructions[15].OpCode == CilOpCodes.Call; // 15	0020	call	instance int64 VM::DecryptPosition(string)

    public MethodDiscovery(DevirtualizationContext ctx) : base(ctx)
    {
    }
}