using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDisassembler : Stage
{
    public override bool Run()
    {
        using var reader = new VMBinaryReader(new CryptoStreamV3(Ctx.VMStream, Ctx.MethodCryptoKey, true));
        var lengthReader = new VMBinaryReader(new CryptoStreamV3(Ctx.VMStream, 0, true));
        var length = lengthReader.ReadInt32();
        lengthReader.Dispose();

        foreach (var vmMethod in Ctx.VMMethods)
        {
           if (vmMethod.EncodedMethodKey != @"5<]fEBf\76") continue;

            vmMethod.MethodKey = VMCipherStream.DecodeMethodKey(vmMethod.EncodedMethodKey, Ctx.PositionCryptoKey);
            
            var position = Ctx.VMStream.Length - (length - vmMethod.MethodKey) - 0xFF;
            // Ctx.VMStream.RsaDecryptBlock(position);

            Ctx.VMStream.Seek(position, SeekOrigin.Begin); // position + 11 /* PKSC1 Header length */ + 0x20
            
            // var bytes = new byte[0x100];
            // var num = Ctx.VMStream.Read(bytes, 0, 0x100);
            //var decryptedBytes = Ctx.VMStream.ReadRSABlock(position);
            var decryptedBytes = new byte[0x100];
            Ctx.VMStream.Read(decryptedBytes, 0, 0x100);
            
            using var rr = new VMBinaryReader(new CryptoStreamV3(new MemoryStream(decryptedBytes), Ctx.MethodCryptoKey, true));
            var a = rr.ReadInt32();
            
            Ctx.Console.Info(a);
        }

        return false;
    }
    
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
}