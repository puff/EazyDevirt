using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;

namespace EazyDevirt.Core.IO;

public class CryptoStreamV3 : Stream
{
    private Stream _stream;
    private int _key;
    private int _modulus;
    private RSAParameters _rsaParameters;

    public CryptoStreamV3(Stream stream, int key, RSAParameters? rsaParams = null)
    {
        _stream = stream;
        _key = key ^
               -559030707; // TODO: find this constant automatically (though in all samples i've analyzed, this key is the same)

        _rsaParameters = rsaParams.GetValueOrDefault();
    }

    
    
    private byte[] RSAPublicDecrypt(byte[] encryptedData)
    {
        // TODO: this don't work. migrate to bouncy castle or implement custom BigInteger
        var encData = new BigInteger(encryptedData);
        var bnData =  BigInteger.ModPow(encData, new BigInteger(_rsaParameters.Exponent!)
            , new BigInteger(_rsaParameters.Modulus!));

        var mostSignificantBit = bnData.GetBitLength();
        
        return bnData.ToByteArray(false, true);
    }
    
    public void DecryptBlock(int pos)
    {
        if (_rsaParameters.Modulus == null) return;

        // Console.WriteLine(Convert.ToBase64String(_rsa.ExportRSAPrivateKey()));
        
        var oldPos = this.Position;
        this.Position = pos;
        
        const int blockSize = 0x100; // 256
        
        var blockBuffer = new byte[blockSize];
        var read = _stream.Read(blockBuffer, 0, blockSize);
        if (read != blockSize) return;

        this.Position = pos;
        var decrypted = RSAPublicDecrypt(blockBuffer);
        _stream.Write(decrypted, 0, blockSize);
        this.Position = oldPos;
    }
    
    private byte Decrypt(byte byte_0, uint uint_0)
    {
        var b = (byte)(_key ^ (int)uint_0);
        return (byte)(byte_0 ^ b);
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        var num = (uint)_stream.Position;
        var num2 = _stream.Read(buffer, offset, count);
        var num3 = offset + num2;
        for (var i = offset; i < num3; i++)
            buffer[i] = Decrypt(buffer[i], num++);
        return num2;
    }
    
    public override void Write(byte[] buffer, int offset, int count)
    {
        var num = (uint)_stream.Position;
        var array = new byte[count];
        var num2 = 0U;
        while (num2 < (ulong)count)
        {
            array[num2] = Decrypt(buffer[(int)(IntPtr)unchecked(num2 + (ulong)offset)], num + num2);
            num2++;
        }

        _stream.Write(array, 0, count);
    }


    public override void Flush()
    {
        _stream.Flush();
    }
    
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }
}