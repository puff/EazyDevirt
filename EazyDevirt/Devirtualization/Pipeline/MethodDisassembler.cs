using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDisassembler : Stage
{
    public override bool Run()
    {
        var reader = new VMBinaryReader(new CryptoStreamV2(Ctx.VMStream, Ctx.MethodCryptoKey));
        var lengthReader = new VMBinaryReader(new CryptoStreamV2(Ctx.VMStream, 0));
        var length = lengthReader.ReadInt32();

        foreach (var vmMethod in Ctx.VMMethods)
        {
           // if (vmMethod.EncodedMethodKey != @"5<]fEBf\76") continue;

            vmMethod.MethodKey = VMStream.DecodeMethodKey(vmMethod.EncodedMethodKey, Ctx.PositionCryptoKey);
            reader.SetMethodKey(vmMethod.MethodKey);

            // if (vmMethod.MethodKey != 0x1C55) continue;
            // var raw_position = Ctx.VMStream.Length - (length - vmMethod.MethodKey) - 0xFF;
            // this is not exactly correct, MethodKey can be different. MD Token for debugging: 0x0600053E
            var raw_position = (long)(4 + Math.Ceiling((double)vmMethod.MethodKey / 0xF5) * 0x100); 
            // Ctx.VMStream.RsaDecryptBlock(raw_position);

            // Ctx.VMStream.Seek(position + 11 /* PKSC1 Header length */ + 0x20 /* 32 */ , SeekOrigin.Begin);
            
            var first = reader.ReadInt32();
            Ctx.Console.Info(vmMethod.MethodKey.ToString("X") + " " + raw_position.ToString("X"));
        }

        return false;
    }
    
    public MethodDisassembler(DevirtualizationContext ctx) : base(ctx)
    {
    }
}