using Org.BouncyCastle.Math;

namespace EazyDevirt.Core.IO;

/// <summary>
/// Wrapper for the cipher stream that reads VM resource data.
/// </summary>
// TODO: Remove this todo. This is Stream1 in the sample.
internal class VMStream : Stream
{
    private int _length { get; set; }
    private int _position { get; set; }
    
    private VMCipherStream _cipherStream { get; }

    public VMStream(byte[] buffer, BigInteger mod, BigInteger exp)
    {
        _cipherStream = new VMCipherStream(buffer, mod, exp);
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (_length - _position > 0 && origin == SeekOrigin.Current)
            offset -= _length - _position;
        
        var position = Position;
        var num = _cipherStream.Seek(offset, origin);
        _position = (int)(num - (position - _position));
        if (0 <= _position && _position < _length)
            _cipherStream.Seek(_length - _position, SeekOrigin.Current);
        else
            _length = 0;
            _position = 0;
            
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