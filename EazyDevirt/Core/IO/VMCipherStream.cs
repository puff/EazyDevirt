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
    #region Fields
    /// <summary>
    /// Rsa/PKSC1 input block size.
    /// </summary>
    private const int InputBlockSize = 0x100;
    
    /// <summary>
    /// Rsa/PKSC1 output block size.
    /// </summary>
    private const int OutputBlockSize = 0xF5; // TODO: Verify this value is correct.

    /// <summary>
    /// Inner resource stream
    /// </summary>
    private MemoryStream ResourceStream { get; }

    /// <summary>
    /// Rsa/PKSC1 engine for decrypting data.
    /// </summary>
    private Pkcs1Encoding Rsa { get; }
    
    /// <summary>
    /// This is used to calculate the new position.
    /// </summary>
    /// <remarks>
    /// This is equal to (Position / OutputBlockSize)
    /// </remarks>
    private int PositionPart1 { get; set; }

    /// <summary>
    /// This is used to calculate the new position.
    /// </summary>
    /// <remarks>
    /// This is equal to (Position % OutputBlockSize)
    /// </remarks>
    private int PositionPart2 { get; set; }
    
    /// <summary>
    /// Whether to set the inner resource stream's position during an Rsa read.
    /// </summary>
    private bool SetResourcePosition { get; set; }
    
    /// <summary>
    /// Whether the Rsa portion was already read.
    /// </summary>
    private bool AlreadyReadRsa { get; set; }
    
    /// <summary>
    /// Whether the Rsa read faled
    /// </summary>
    private bool RsaReadFailed { get; set; }
    
    /// <summary>
    /// Amount of bytes read in the Rsa read function.
    /// </summary>
    /// <remarks>
    /// TODO: find out if this is what it actually is. Probably not.
    /// </remarks>
    private int RsaBytesRead { get; set; }
    
    /// <summary>
    /// Whether the length of the stream was initialized.
    /// </summary>
    private bool LengthInitialized { get; set; }
    
    /// <summary>
    /// This exists because we cannot directly set the stream's length.
    /// </summary>
    private int _Length { get; set; }
    
    /// <summary>
    /// This is used to calculate the position and number of bytes to read.
    /// </summary>
    /// <remarks>
    /// This is equal to (Length / OutputBlockSize)
    /// </remarks>
    private int LengthPart1 { get; set; }

    /// <summary>
    /// This is used to calculate the position and number of bytes to read.
    /// </summary>
    /// <remarks>
    /// This is equal to (Length % OutputBlockSize)
    /// </remarks>
    private int LengthPart2 { get; set; }
    
    private byte[] InputBlockBuffer { get; }
    private byte[] OutputBlockBuffer { get; set; }
    
    #endregion Fields
    
    public VMCipherStream(byte[] buffer, BigInteger mod, BigInteger exp)
    {
        // TODO: May want to move this to using an IBufferedCipher like CipherStream.
        //       var rsaCipher = CipherUtilities.GetCipher("Rsa/ECB/PKCS1");

        ResourceStream = new MemoryStream(buffer);
        InputBlockBuffer = new byte[InputBlockSize];
        OutputBlockBuffer = new byte[OutputBlockSize];
        
        var rsaEngine = new RsaEngine();
        Rsa = new Pkcs1Encoding(rsaEngine);
        Rsa.Init(false, new RsaKeyParameters(true /* The key is public, but the PKSC1 encoding requires this to work correctly. */, mod, exp));
    }
    
    // TODO: Move this somewhere else.
    public static long DecodeMethodKey(string positionString, int positionKey)
    {
        var decoded = Ascii85.FromAscii85String(positionString);

        using var reader = new VMBinaryReader(new CryptoStreamV3(new MemoryStream(decoded), positionKey));
        return reader.ReadInt64();
    }

    private bool ReadAndProcessRsaBlock(int int_8)
    {
        var i = 0;
        while (i < InputBlockSize)
        {
            var num = ResourceStream.Read(InputBlockBuffer, i, InputBlockSize - i);
            if (num != 0)
                i += num;
            else
            {
                if (i != 0)
                    throw new InvalidOperationException();
                
                RsaReadFailed = true;
                return false;
            }
        }
        OutputBlockBuffer = Rsa.ProcessBlock(InputBlockBuffer, 0, InputBlockSize);
        RsaBytesRead = OutputBlockBuffer.Length;
        if (int_8 == LengthPart1)
            RsaBytesRead = LengthPart2;
        
        return true;   
    }
    
    private void ReadRsaBlock()
    {
        if (!LengthInitialized) InitializeLength();
        if (!AlreadyReadRsa)
        {
            AlreadyReadRsa = true;
            RsaReadFailed = false;
            var num = PositionPart1;
            if (SetResourcePosition)
            {
                ResourceStream.Position = 4 + num * InputBlockSize;
                SetResourcePosition = false;
            }

            ReadAndProcessRsaBlock(num);
        }
    }

    private void ReadRsaBlockAndMore()
    {
        var num = PositionPart1 + 1;
        if (ReadAndProcessRsaBlock(num))
        {
            PositionPart1 = num;
            PositionPart2 = 0;
        }

        AlreadyReadRsa = true;
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Less than 0");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Less than 0");
        if (buffer.Length - offset < count)
            throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Length - offset, "Less than count with offset factored in");
        if (count == 0)
            return 0;

        var i = count;
        var num = offset;
        if (PositionPart2 < OutputBlockSize)
        {
            ReadRsaBlock();
            var num2 = RsaBytesRead - PositionPart2;
            if (num2 > count)
            {
                Buffer.BlockCopy(OutputBlockBuffer, PositionPart2, buffer, offset, count);
                PositionPart2 += count;
                return count;
            }
            Buffer.BlockCopy(OutputBlockBuffer, PositionPart2, buffer, offset, count);
            PositionPart2 = RsaBytesRead;
            if (RsaReadFailed)
                return num2;
            i -= num2;
            num += num2;
        }

        if (RsaReadFailed)
            return count - i;

        while (i > 0)
        {
            ReadRsaBlockAndMore();
            if (RsaReadFailed)
                return count - i;

            var num3 = RsaBytesRead;
            if (i < num3)
            {
                Buffer.BlockCopy(OutputBlockBuffer, 0, buffer, num, i);
                PositionPart2 = i;
                return count;
            }
            Buffer.BlockCopy(OutputBlockBuffer, 0, buffer, num, num3);
            num += num3;
            i -= num3;
            PositionPart2 = num3;
        }

        return count;
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

    private void InitializeLength()
    {
        if (LengthInitialized) return;
        
        if (ResourceStream.Position != 0L)
        {
            ResourceStream.Position = 0L;
            SetResourcePosition = true;
        }

        using var cryptoStream = new CryptoStreamV3(ResourceStream, 0, true);
        using var lengthBinaryReader = new VMBinaryReader(cryptoStream);

        _Length = lengthBinaryReader.ReadInt32();
        LengthPart1 = _Length / OutputBlockSize;
        LengthPart2 = _Length % OutputBlockSize;
        LengthInitialized = true;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    
    public override long Length
    {
        get
        {
            if (!LengthInitialized) InitializeLength();
            return _Length;
        }
    }

    public override long Position
    {
        get => PositionPart1 * OutputBlockSize + PositionPart2;
        set
        {
            var num = (int)value / OutputBlockSize;
            PositionPart2 = (int)value % OutputBlockSize;
            if (PositionPart1 == num) return;
            
            PositionPart1 = num;
            SetResourcePosition = true;
            AlreadyReadRsa = false;
        }
    }
}