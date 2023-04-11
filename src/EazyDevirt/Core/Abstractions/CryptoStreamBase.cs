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
        var num = (uint)_stream.Position;
        var num2 = _stream.Read(buffer, offset, count);
        var num3 = offset + num2;
        for (var i = offset; i < num3; i++)
            buffer[i] = Crypt(buffer[i], num++);
        return num2;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var num = (uint)_stream.Position;
        var array = new byte[count];
        var num2 = 0U;
        while (num2 < (ulong)count)
        {
            array[num2] = Crypt(buffer[(int)(IntPtr)unchecked(num2 + (ulong)offset)], num + num2);
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

    public override void Close()
    {
        if (!_leaveOpen)
            _stream.Close();

        _stream = null!;
        base.Close();
    }

    public override bool CanRead
    {
        get => _stream.CanRead;
    }

    public override bool CanSeek
    {
        get => _stream.CanSeek;
    }

    public override bool CanWrite
    {
        get => _stream.CanWrite;
    }

    public override long Length
    {
        get => _stream.Length;
    }

    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }
    #endregion
}