using EazyDevirt.Util;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace EazyDevirt.Core.IO;

internal class VMStream : MemoryStream
{
    /// <summary>
    /// This is constant in the CryptoStream's ctor method.
    /// </summary>
    /// <remarks>
    /// This value is -559030707 in two's complements.
    /// </remarks>
    private const uint KeyXor = 0xDEADDE4D;

    private Pkcs1Encoding Rsa { get; }
    
    // private BigInteger Modulus { get; }
    // private BigInteger Exponent { get; }

    public VMStream(byte[] buffer, BigInteger mod, BigInteger exp) : base(buffer)
    {
        // Modulus = mod;
        // Exponent = exp;
        var rsaEngine = new RsaEngine();
        Rsa = new Pkcs1Encoding(rsaEngine);
        Rsa.Init(false, new RsaKeyParameters(false, mod, exp));
    }

    public bool RsaDecryptBlock(long position)
    {
        const int blockSize = 0x100;

        // var oldPos = base.Position;
        base.Position = position;
        var blockBuffer = new byte[blockSize];
        var read = base.Read(blockBuffer, 0, blockSize);
        if (read != blockSize) return false;

        // var decrypted = RsaPublicCrypt(blockBuffer);
        Console.WriteLine(Rsa.GetOutputBlockSize() + " " + Rsa.GetInputBlockSize());
        var decrypted = Rsa.ProcessBlock(blockBuffer, 0, blockSize);
        
        base.Position = position;
        var zeroLength = blockSize - decrypted.Length;
        if (zeroLength > 0)
        {
            var zeroes = new byte[zeroLength];
            Array.Fill(zeroes, (byte)0);
            base.Write(zeroes, 0, zeroLength);
        }
        
        base.Write(decrypted, 0, decrypted.Length);
        
        return true;
    }
    
    public static long DecodeMethodKey(string positionString, int positionKey)
    {
        var decoded = Ascii85.FromAscii85String(positionString);

        var reader = new VMBinaryReader(new CryptoStreamV2(new MemoryStream(decoded), positionKey));
        return reader.ReadInt64();
    }
}