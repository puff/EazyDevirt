using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Logging;

namespace EazyDevirt.Devirtualization.Pipeline;

internal sealed class MethodDiscovery : StageBase
{
    public MethodDiscovery(Context ctx) : base(ctx)
    {
    }

    private protected override bool Init()
    {
        var module = Ctx.Module;

        var methods = module
            .GetAllTypes()
            .SelectMany(t => t.Methods)
            .Where(m => m.CilMethodBody != null);


        foreach (var method in methods)
        {
            if (!IsLoadVMPositionMethod(method))
                continue;

            var cilMethodBody = method.CilMethodBody;
            var instructions = cilMethodBody?.Instructions;

            // The instruction indices are the same across all samples I've analyzed.
            if (instructions?[1].Operand is SerializedMethodDefinition cryptoKeyMethod)
            {
                if (!IsCryptoKeyMethod(cryptoKeyMethod))
                    continue;

                var cryptoKeyBody = cryptoKeyMethod.CilMethodBody;
                var cryptoKeyInstructions = cryptoKeyBody!.Instructions;

                Ctx.MethodCryptoKey = cryptoKeyInstructions[0].GetLdcI4Constant();

                Logger.Success("Found VM method crypto key!", VerboseLevel.Verbose);
                Logger.InfoStr("VM Method Crypto Key", Ctx.MethodCryptoKey, VerboseLevel.Verbose);

                // this should be in the same method
                if (instructions?[15].Operand is SerializedMethodDefinition decryptVMPositionMethod)
                {
                    var vmPositionBody = decryptVMPositionMethod.CilMethodBody;
                    var vmPositionInstructions = vmPositionBody!.Instructions;

                    if (!IsDecryptPositionMethod(decryptVMPositionMethod))
                    {
                        Logger.Error("Failed to find VM position decrypt method.");
                        return false;
                    }

                    if (vmPositionInstructions[6].Operand is SerializedMethodDefinition
                        positionCryptoKeyMethod)
                    {
                        var positionCryptoKeyBody = positionCryptoKeyMethod.CilMethodBody;
                        var positionCryptoInstr = positionCryptoKeyBody!.Instructions;

                        if (!IsCryptoKeyMethod(positionCryptoKeyMethod))
                        {
                            Logger.Error("Failed to find VM position crypto key.");
                            return false;
                        }

                        Ctx.PositionCryptoKey = positionCryptoInstr[0].GetLdcI4Constant();

                        Logger.Success("Found VM position crypto key!", VerboseLevel.Verbose);
                        Logger.InfoStr("VM Position Crypto Key", Ctx.PositionCryptoKey, VerboseLevel.VeryVerbose);
                    }
                }
            }

            break;
        }

        if (Ctx.MethodCryptoKey == 0)
        {
            Logger.Error("Failed to find vm method crypto key.");
            return false;
        }

        Ctx.VMMethods = new List<VMMethod>();
        return true;
    }

    public override bool Run()
    {
        if (!Init())
            return false;

        var module = Ctx.Module;

        var methods = module
            .GetAllTypes()
            .SelectMany(t => t.Methods)
            .Where(m => m.CilMethodBody != null);

        // TODO: Use echo?
        foreach (var method in methods)
        {
            var cilMethodBody = method.CilMethodBody;
            if (cilMethodBody is null)
                continue;

            var instructions = cilMethodBody.Instructions;
            var index = -1;

            for (var i = 0; i < instructions.Count; i++)
            {
                var ins = instructions[i];

                if (ins.OpCode.Code is not CilCode.Call ||
                    ins.Operand is not SerializedMethodDefinition operand ||
                    operand.MetadataToken != Ctx.VMResourceGetterMdToken)
                    continue;

                index = i;
                break;
            }

            //[0] = {CilInstruction} IL_0000: call VM VMGetter::GetVMInstance()
            //[1] = {CilInstruction} IL_0005: call System.IO.Stream VMGetter::GetVMResourceStream()                         // this is the one we want
            //[2] = {CilInstruction} IL_000A: ldstr "5<^5Q+Z_VC"                                                            // this is the one we want
            //[3] = {CilInstruction} IL_000F: ldnull
            //[4] = {CilInstruction} IL_0010: call System.Object VM::ExecuteVMPosition(System.IO.Stream, System.String, System.Object[])
            //[5] = {CilInstruction} IL_0015: unbox System.Int32
            //[6] = {CilInstruction} IL_001A: ldobj System.Int32
            //[7] = {CilInstruction} IL_001F: ret

            if (index == -1)
                continue;

            // hack fix for virtualized methods using out parameters
            if (instructions[index + 1].IsStloc())
                index += 3;

            if (instructions[index + 1].OpCode.Code is not CilCode.Ldstr)
            {
                if (Ctx.Options.Verbose)
                    Logger.Error($"Expected ldstr on instruction {index + 1} for method {method.MetadataToken}");
                continue;
            }

            if (instructions[index + 1].Operand is not string encodedMethodKey)
            {
                Logger.Error($"Failed to get encoded method key for method {method.MetadataToken}");
                continue;
            }

            Logger.InfoStr("Virtualized method found", method.MetadataToken, VerboseLevel.VeryVerbose);
            Ctx.VMMethods.Add(new VMMethod(method, encodedMethodKey));
        }

        Logger.Success($"Discovered {Ctx.VMMethods.Count} virtualized methods!", VerboseLevel.Verbose);

        return true;
    }

    private static bool IsDecryptPositionMethod(MethodDefinition method)
    {
        return method.Signature?.ReturnType.FullName == typeof(long).FullName
               && method.Parameters.Count == 1
               && method.Parameters[0].ParameterType.FullName == typeof(string).FullName
               && method.CilMethodBody != null
               && method.CilMethodBody.Instructions[6].OpCode ==
               CilOpCodes.Call; // 6	000E	call	instance int32 VM::GetVMPositionCryptoKey()
    }

    private static bool IsCryptoKeyMethod(MethodDefinition method)
    {
        return method.Signature?.ReturnType.FullName == typeof(int).FullName
               && method.CilMethodBody is { Instructions.Count: 2 }
               && method.CilMethodBody.Instructions[0].IsLdcI4();
    }

    private static bool IsLoadVMPositionMethod(MethodDefinition method)
    {
        return method is { IsStatic: false, Signature.ReturnsValue: false, Parameters.Count: 3 }
               && method.Parameters[0].ParameterType.FullName == typeof(Stream).FullName
               && method.Parameters[1].ParameterType.FullName == typeof(long).FullName
               && method.Parameters[2].ParameterType.FullName == typeof(string).FullName
               && method.CilMethodBody is not null
               && method.CilMethodBody.Instructions[1].OpCode ==
               CilOpCodes.Call // 1	    0001	call	instance int32 VM::GetVMMethodCryptoKey()
               && method.CilMethodBody.Instructions[15].OpCode ==
               CilOpCodes.Call; // 15	0020	call	instance int64 VM::DecryptPosition(string)
    }
}