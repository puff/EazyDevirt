using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDisassembler : Stage
{
    public override bool Run()
    {
        using var reader = new VMBinaryReader(new CryptoStreamV3(Ctx.VMStream, Ctx.MethodCryptoKey, true));

        foreach (var vmMethod in Ctx.VMMethods)
        {
           // if (vmMethod.EncodedMethodKey != @"5<]fEBf\76") continue;
           //if (vmMethod.EncodedMethodKey != @"5<_4mf/boO") continue;

            vmMethod.MethodKey = VMCipherStream.DecodeMethodKey(vmMethod.EncodedMethodKey, Ctx.PositionCryptoKey);

            using var vmStream = new CryptoStreamV3(Ctx.VMStream, Ctx.MethodCryptoKey, true);
            vmStream.Seek(vmMethod.MethodKey, SeekOrigin.Begin);

            using var vmMethodReader = new VMBinaryReader(vmStream);
            vmMethod.MethodInfo = new VMMethodInfo(vmMethodReader);

            Ctx.Console.Info(vmMethod.MethodInfo.ToString());
        }

        return false;
    }
    
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
}