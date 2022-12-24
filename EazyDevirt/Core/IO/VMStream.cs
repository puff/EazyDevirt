using Org.BouncyCastle.Math;

namespace EazyDevirt.Core.IO;

/// <summary>
/// Wrapper for the cipher stream that reads VM resource data.
/// </summary>
// TODO: Remove this todo. This is Stream1 in the sample.
internal class VMStream : Stream
{
    private int _Length { get; set; }
    private int _Position { get; set; }
    
    private VMCipherStream CipherStream { get; }

    public VMStream(byte[] buffer, BigInteger mod, BigInteger exp)
    {
        CipherStream = new VMCipherStream(buffer, mod, exp);
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
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