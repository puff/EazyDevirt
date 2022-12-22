using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDisassembler : Stage
{
    public override bool Run()
    {
        using var reader = new VMBinaryReader(new CryptoStreamV3(Ctx.VMResourceStream, Ctx.MethodCryptoKey, true));
        var lengthReader = new VMBinaryReader(new CryptoStreamV3(Ctx.VMResourceStream, 0, true));
        var length = lengthReader.ReadInt32();
        lengthReader.Dispose();

        foreach (var vmMethod in Ctx.VMMethods)
        {
           if (vmMethod.EncodedMethodKey != @"5<]fEBf\76") continue;

            vmMethod.MethodKey = VMStream.DecodeMethodKey(vmMethod.EncodedMethodKey, Ctx.PositionCryptoKey);
            
            var position = Ctx.VMResourceStream.Length - (length - vmMethod.MethodKey) - 0xFF;
            // Ctx.VMResourceStream.RsaDecryptBlock(position);

            Ctx.VMResourceStream.Seek(position + 11 /* PKSC1 Header length */ + 0x20 , SeekOrigin.Begin);
            
            var first = reader.ReadInt32();
            Ctx.Console.Info(first);
        }

        return false;
    }
    
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
}