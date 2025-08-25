using System;
using System.IO;

namespace EazyDevirt.Core.Crypto;

internal sealed class HMDecryptor : HMEncryptionChain
{
    public HMDecryptor(byte[] password, long salt) : base(password, salt)
    {
    }

    public byte[] DecryptInstructionBlock(Stream instructionsStream)
    {
        // Read first 4 bytes (encrypted header containing original length)
        var header = new byte[4];
        ReadBytes(instructionsStream, header, 0, 4);

        // Decrypt header to obtain original size
        var decryptedHeader = DecryptBytes(header, startWithEncrypt: false);
        var originalSize = ConvertInt32BytesToLittleEndian(decryptedHeader, 0);

        // Total encrypted size is aligned to 4 and includes 4-byte header
        var alignedTotal = MinAlignToMultipleOf4(originalSize);
        var remaining = alignedTotal - 4;

        var fullBlock = new byte[alignedTotal];
        Buffer.BlockCopy(header, 0, fullBlock, 0, 4);

        // Read remaining encrypted bytes
        ReadBytes(instructionsStream, fullBlock, 4, remaining);

        // Decrypt full block then strip 4-byte header
        var decrypted = DecryptBytes(fullBlock, startWithEncrypt: false);
        var result = new byte[originalSize];
        Buffer.BlockCopy(decrypted, 4, result, 0, originalSize);
        return result;
    }

    private static void ReadBytes(Stream stream, byte[] buffer, int offset, int count)
    {
        var remaining = count;
        while (remaining > 0)
        {
            var read = stream.Read(buffer, offset, remaining);
            if (read <= 0)
                throw new EndOfStreamException("Unexpected end of stream while reading encrypted homomorphic block.");
            offset += read;
            remaining -= read;
        }
    }
}
