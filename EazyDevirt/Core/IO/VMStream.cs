using Org.BouncyCastle.Math;

namespace EazyDevirt.Core.IO;

/// <summary>
/// Wrapper for the cipher stream that reads VM resource data.
/// </summary>
internal class VMStream : Stream
{
    private byte[] _Buffer { get; set; }
    private int _Length { get; set; }
    private int _Position { get; set; }
    
    private VMCipherStream CipherStream { get; }

    public VMStream(byte[] buffer, BigInteger mod, BigInteger exp)
    {
        CipherStream = new VMCipherStream(buffer, mod, exp);
    }
    
    // Modified MemoryStream read
    private int ReadDefault(byte[] buffer, int offset, int count)
    {
        var num = _Length - _Position;
        if (num <= 0)
            return 0;

        if (num > count)
            num = count;
        
        Buffer.BlockCopy(_Buffer, _Position, buffer, offset, num);
        _Position += num;
        return num;
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Less than 0");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Less than 0");
        if (buffer.Length - offset < count)
            throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Length - offset, "Less than count with offset factored in");

        var num = offset;
        var num2 = ReadDefault(buffer, offset, count);
        if (num2 == count)
            return num2;

        var num3 = num2;
        if (num2 > 0)
        {
            count -= num2;
            offset += num2;
        }

        _Length = 0;
        _Position = 0;
        
        // TODO: The fun part begins here.
        
        return CipherStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (_Length - _Position > 0 && origin == SeekOrigin.Current)
            offset -= _Length - _Position;
        
        var position = Position;
        var num = CipherStream.Seek(offset, origin);
        _Position = (int)(num - (position - _Position));
        if (0 <= _Position && _Position < _Length)
            CipherStream.Seek(_Length - _Position, SeekOrigin.Current);
        else
        {
            _Length = 0;
            _Position = 0;
        }

        return num;
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

    public override long Length => CipherStream.Length;

    public override long Position
    {
        get => CipherStream.Position + (_Position - _Length);
        set => Seek(value, SeekOrigin.Begin);
    }
}