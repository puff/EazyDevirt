using EazyDevirt.Util;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace EazyDevirt.Core.IO;

/// <summary>
/// Cipher stream used for reading VM resource data.
/// </summary>
// TODO: Make this into a modified CipherStream. In sample, this is the inner stream (Stream2). It looks very similar to a CipherStream.
internal class VMCipherStream : Stream
{
    private Pkcs1Encoding Rsa { get; }
    
    public VMCipherStream(byte[] buffer, BigInteger mod, BigInteger exp)
    {
        // TODO: May have to move this to using an IBufferedCipher like CipherStream.
        // var rsaCipher = CipherUtilities.GetCipher("RSA/ECB/PKCS1");
        
        var rsaEngine = new RsaEngine();
        Rsa = new Pkcs1Encoding(rsaEngine);
        Rsa.Init(false, new RsaKeyParameters(false /* true */, mod, exp));
    }

    public byte[] ReadRSABlock(long position)
    {
        const int inputBlockSize = 0x100; // Rsa.GetInputBlockSize() 
        const int outputBlockSize = 0xFF; // Rsa.GetOutputBlockSize()
        
        Seek(position, SeekOrigin.Begin);

        // TODO: Add the while loop that's used for checking if read bytes are less than output block size.
        
        var bytes = new byte[inputBlockSize];
        var num = Read(bytes, 0, inputBlockSize);
        
        var decryptedBytes = Rsa.ProcessBlock(bytes, 0, inputBlockSize);
        return decryptedBytes;
    }

    // TODO: Move this somewhere else.
    public static long DecodeMethodKey(string positionString, int positionKey)
    {
        var decoded = Ascii85.FromAscii85String(positionString);

        using var reader = new VMBinaryReader(new CryptoStreamV3(new MemoryStream(decoded), positionKey));
        return reader.ReadInt64();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length + offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }
        return Position;
    }

    public override void Flush()
    {
    }
    
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length
    {
        get
        {
            return Length;
        }
    }

    public override long Position
    {
        get
        {
            return Position;
        }
        set
        {
            Position = value;
        }
    }
}