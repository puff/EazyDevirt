using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace EazyDevirt.Core.IO;

/// <summary>
/// Cipher stream used for reading VM resource data.
/// </summary>
/// <remarks>
/// This is the inner stream when you decompile.
/// The outer stream seems to just be a wrapper with a cache.
/// Looks to be the same across Eazfuscator versions.
/// </remarks>
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
    private const int OutputBlockSize = 0xF5;

    /// <summary>
    /// Inner resource stream.
    /// </summary>
    public MemoryStream ResourceStream { get; }

    /// <summary>
    /// Rsa/PKSC1 engine for decrypting data.
    /// </summary>
    private Pkcs1Encoding Rsa { get; }
    
    /// <summary>
    /// The id of the current block being read.
    /// </summary>
    /// <remarks>
    /// This is equal to (Position / OutputBlockSize)
    /// </remarks>
    private int BlockId { get; set; }

    /// <summary>
    /// The current offset into the block that is being read.
    /// </summary>
    /// <remarks>
    /// This is equal to (Position % OutputBlockSize)
    /// </remarks>
    private int BlockOffset { get; set; }
    
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
    /// This is the last block to read.
    /// </summary>
    /// <remarks>
    /// This is equal to (Length / OutputBlockSize)
    /// </remarks>
    private int LastBlockId { get; set; }

    /// <summary>
    /// This is the last offset in the last block.
    /// </summary>
    /// <remarks>
    /// This is equal to (Length % OutputBlockSize)
    /// </remarks>
    private int LastBlockOffset { get; set; }
    
    private byte[] InputBlockBuffer { get; }
    private byte[] OutputBlockBuffer { get; set; }

    /// <summary>
    /// Cache already read blocks.
    /// </summary>
    private Dictionary<int, byte[]> Blocks { get; }

    #endregion Fields
    
    public VMCipherStream(byte[] buffer, BigInteger mod, BigInteger exp)
    {
        ResourceStream = new MemoryStream(buffer);
        InputBlockBuffer = new byte[InputBlockSize];
        OutputBlockBuffer = new byte[OutputBlockSize];
        Blocks = new Dictionary<int, byte[]>();
        
        var rsaEngine = new RsaEngine();
        Rsa = new Pkcs1Encoding(rsaEngine);
        Rsa.Init(false, new RsaKeyParameters(true /* The key is public, but the PKSC1 encoding requires this to work correctly. */, mod, exp));
    }

    private bool ReadAndProcessRsaBlock(int blockId)
    {
        if (Blocks.TryGetValue(blockId, out var block))
            OutputBlockBuffer = block;
        else
        {
            var offset = 0;
            while (offset < InputBlockSize)
            {
                var bytesRead = ResourceStream.Read(InputBlockBuffer, offset, InputBlockSize - offset);
                if (bytesRead != 0)
                    offset += bytesRead;
                else
                {
                    if (offset != 0)
                        throw new InvalidOperationException();

                    RsaReadFailed = true;
                    return false;
                }
            }
            OutputBlockBuffer = Rsa.ProcessBlock(InputBlockBuffer, 0, InputBlockSize);
            Blocks[blockId] = OutputBlockBuffer;
        }
        
        RsaBytesRead = OutputBlockBuffer.Length;
        if (blockId == LastBlockId)
            RsaBytesRead = LastBlockOffset;
        
        return true;   
    }
    
    private void ReadRsaBlock()
    {
        if (!LengthInitialized) InitializeLength();
        if (AlreadyReadRsa) return;
        
        AlreadyReadRsa = true;
        RsaReadFailed = false;
        if (SetResourcePosition)
        {
            ResourceStream.Position = 4 + BlockId * InputBlockSize;
            SetResourcePosition = false;
        }

        ReadAndProcessRsaBlock(BlockId);
    }

    private void ReadNextRsaBlock()
    {
        var nextBlockId = BlockId + 1;
        if (ReadAndProcessRsaBlock(nextBlockId))
        {
            BlockId = nextBlockId;
            BlockOffset = 0;
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
        
        var bytesLeftToRead = count;
        var newOffset = offset;
        if (BlockOffset < OutputBlockSize)
        {
            ReadRsaBlock();

            // if the amount of bytes to read (count) is within the current block, complete the read
            var bytesUntilNextBlock = RsaBytesRead - BlockOffset;
            if (bytesUntilNextBlock > count)
            {
                Buffer.BlockCopy(OutputBlockBuffer, BlockOffset, buffer, offset, count);
                BlockOffset += count;
                return count;
            }

            // otherwise, read the rest of current block
            Buffer.BlockCopy(OutputBlockBuffer, BlockOffset, buffer, offset, bytesUntilNextBlock);
            bytesLeftToRead -= bytesUntilNextBlock;
            newOffset += bytesUntilNextBlock;
            
            // then go into the next block, and read the remaining bytes needed to complete the read operation
            Position += bytesUntilNextBlock;
            ReadRsaBlock();
            Buffer.BlockCopy(OutputBlockBuffer, offset, buffer, newOffset, bytesLeftToRead);

            Position += count - bytesUntilNextBlock;
            return count;
        }

        if (RsaReadFailed)
            return count - bytesLeftToRead;

        // cycle through blocks and read bytes
        while (bytesLeftToRead > 0)
        {
            ReadNextRsaBlock();
            if (RsaReadFailed)
                return count - bytesLeftToRead;

            var bytesRead = RsaBytesRead;
            if (bytesLeftToRead < bytesRead)
            {
                Buffer.BlockCopy(OutputBlockBuffer, 0, buffer, newOffset, bytesLeftToRead);
                BlockOffset = bytesLeftToRead;
                return count;
            }
            Buffer.BlockCopy(OutputBlockBuffer, 0, buffer, newOffset, bytesRead);
            newOffset += bytesRead;
            bytesLeftToRead -= bytesRead;
            BlockOffset = bytesRead;
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
        LastBlockId = _Length / OutputBlockSize;
        LastBlockOffset = _Length % OutputBlockSize;
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
        get => BlockId * OutputBlockSize + BlockOffset;
        set
        {
            var blockId = (int)value / OutputBlockSize;
            BlockOffset = (int)value % OutputBlockSize;
            if (BlockId == blockId) return;
            
            BlockId = blockId;
            SetResourcePosition = true;
            AlreadyReadRsa = false;
        }
    }
}