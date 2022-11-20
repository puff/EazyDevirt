using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDisassembler : Stage
{
    public override bool Run()
    {
        var reader = new VMBinaryReader(new CryptoStreamV2(Ctx.VMResourceStream, Ctx.MethodCryptoKey));
        var lengthReader = new VMBinaryReader(new CryptoStreamV2(Ctx.VMResourceStream, 0));
        var length = lengthReader.ReadInt32();

        foreach (var vmMethod in Ctx.VMMethods)
        {
           // if (vmMethod.EncodedMethodKey != @"5<]fEBf\76") continue;

            vmMethod.MethodKey = VMStream.DecodeMethodKey(vmMethod.EncodedMethodKey, Ctx.PositionCryptoKey);
            reader.SetMethodKey(vmMethod.MethodKey);
            
            var position = Ctx.VMResourceStream.Length - (length - vmMethod.MethodKey) - 0xFF;
            Ctx.VMResourceStream.RsaDecryptBlock(position);

            Ctx.VMResourceStream.Seek(position + 11 /* PKSC1 Header length */ + 0x20 /* 32 */ , SeekOrigin.Begin);
            
            var first = reader.ReadInt32();
            Ctx.Console.Info(first);
        }

        return false;
    }
    
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
}