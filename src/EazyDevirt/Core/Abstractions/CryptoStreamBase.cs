namespace EazyDevirt.Core.Abstractions;

/// <summary>
/// Abstract base class for cryptographic streams. Implements basic stream functionality and exposes an abstract method for byte-level encryption/decryption.
/// </summary>
public abstract class CryptoStreamBase : Stream
{
    /// <summary>
    /// The key used for encryption/decryption.
    /// </summary>
    protected int Key;

    private Stream _stream;
    private readonly bool _leaveOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoStreamBase"/> class with the specified base stream, key, and whether to leave the base stream open.
    /// </summary>
    /// <param name="baseStream">The base stream to read from or write to.</param>
    /// <param name="key">The key used for encryption/decryption.</param>
    /// <param name="leaveOpen">Whether to leave the base stream open when the cryptographic stream is closed.</param>
    protected CryptoStreamBase(Stream baseStream, int key, bool leaveOpen = false)
    {
        _stream = baseStream;
        _leaveOpen = leaveOpen;
        Key = key;
    }

    /// <summary>
    /// Encrypts/decrypts a byte using the given input key.
    /// </summary>
    /// <param name="inputByte">The byte to encrypt/decrypt.</param>
    /// <param name="inputKey">The input key to use for encryption/decryption.</param>
    /// <returns>The encrypted/decrypted byte.</returns>
    protected abstract byte Crypt(byte inputByte, uint inputKey);

    #region Basic Stream Overrides
    public override int Read(byte[] buffer, int offset, int count)
    {
        var position = (uint)_stream.Position;
        var bytesRead = _stream.Read(buffer, offset, count);
        var endOffset = offset + bytesRead;
        for (var i = offset; i < endOffset; i++)
            buffer[i] = Crypt(buffer[i], position++);
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var position = (uint)_stream.Position;
        var array = new byte[count];
        var i = 0u;
        while (i < count)
        {
            array[i] = Crypt(buffer[i + offset], position + i);
            i++;
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

    public override void Close()
    {
        if (!_leaveOpen)
            _stream.Close();

        _stream = null!;
        base.Close();
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
    #endregion
}