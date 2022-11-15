using System.Security.Cryptography;

namespace EazyDevirt.Architecture;

internal class VMStream2 : Stream
{
    private Stream _stream;
    private RSA _rsa;
    
    public VMStream2(Stream stream, RSA rsa)
    {
        _stream = stream;
        _rsa = rsa;
        // this.method_0();
    }
    
    // private void method_0()
    // {
    //     this.int_4 = this.interface5_0.GetInputBlockSize();
    //     this.byte_0 = new byte[this.int_4];
    //     this.int_7 = this.interface5_0.GetOutputBlockSize();
    //     this.byte_1 = new byte[this.int_7];
    // }

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
        }
        return Position;
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
    
    public override void Flush()
    {
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length
    {
        get
        {
            return 0;
        }
    }
    public override long Position { get; set; }
}