using System.Diagnostics;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;

namespace EazyDevirt.Core.IO;

public class CryptoStreamV2 : Stream
{
    private Stream Stream { get; }
    private int Key { get; }
    private BigInteger? Modulus { get; }
    private BigInteger? Exponent { get; }

    public CryptoStreamV2(Stream stream, int key, BigInteger? modulus = null, BigInteger? exponent = null)
    {
        Stream = stream;
        Key = key ^ -559030707;

        Modulus = modulus;
        Exponent = exponent;
    }

    private byte[] RSAPublicDecrypt(byte[] encryptedData)
    {
        var encData = new BigInteger(encryptedData);
        var bnData = encData.ModPow(Exponent, Modulus);
        
        return bnData.ToByteArray();
    }

    private byte Decrypt(byte byte_0, uint uint_0)
    {
        var b = (byte)(Key ^ (int)uint_0);
        return (byte)(byte_0 ^ b);
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        var num = (uint)Stream.Position;
        var num2 = Stream.Read(buffer, offset, count);
        var num3 = offset + num2;
        for (var i = offset; i < num3; i++)
            buffer[i] = Decrypt(buffer[i], num++);
        return num2;
    }
    
    public override void Write(byte[] buffer, int offset, int count)
    {
        var num = (uint)Stream.Position;
        var array = new byte[count];
        var num2 = 0U;
        while (num2 < (ulong)count)
        {
            array[num2] = Decrypt(buffer[(int)(IntPtr)unchecked(num2 + (ulong)offset)], num + num2);
            num2++;
        }

        Stream.Write(array, 0, count);
    }


    public override void Flush()
    {
        Stream.Flush();
    }
    
    public override long Seek(long offset, SeekOrigin origin)
    {
        return Stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        Stream.SetLength(value);
    }

    public override bool CanRead => Stream.CanRead;

    public override bool CanSeek => Stream.CanSeek;

    public override bool CanWrite => Stream.CanWrite;

    public override long Length => Stream.Length;

    public override long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }
}