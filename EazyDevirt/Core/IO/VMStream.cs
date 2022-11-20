using EazyDevirt.Util;
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
    
    private BigInteger Modulus { get; }
    private BigInteger Exponent { get; }

    public VMStream(Stream stream, BigInteger mod, BigInteger exp)
    {
        
    }
    
    public VMStream(byte[] buffer, BigInteger mod, BigInteger exp) : base(buffer)
    {
        Modulus = mod;
        Exponent = exp;
    }

    public long DecodePositionString(string positionString, int positionKey)
    {
        var decoded = Ascii85.FromAscii85String(positionString);
        positionKey = 846439026; // TODO: Get this automatically

        var reader = new VMBinaryReader(new CryptoStreamV2(new MemoryStream(decoded), positionKey));
        return reader.ReadInt64();
    }
}