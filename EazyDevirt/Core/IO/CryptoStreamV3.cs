namespace EazyDevirt.Core.IO;

/// <summary>
/// Stream used to read data about virtualized methods.
/// </summary>
public class CryptoStreamV3 : Stream
{
    private Stream Stream;
    private int Key;
    private bool LeaveOpen;


    /// <param name="stream">Base stream</param>
    /// <param name="key">Crypto key</param>
    /// <param name="leaveOpen">Determines whether the base stream should be disposed.</param>
    public CryptoStreamV3(Stream stream, int key, bool leaveOpen = false)
    {
        Stream = stream;
        Key = key ^ -559030707;
        LeaveOpen = leaveOpen;
    }

    private byte Crypt(byte byte_0, uint uint_0)
    {
        var b = (byte)(Key ^ (int)uint_0);
        return (byte)(byte_0 ^ b);
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        var num = (uint)Stream.Position;
        var num2 = Stream.Read(buffer, offset, count);
        var num3 = offset + num2;
        for (var i = offset; i < num3; i++)
            buffer[i] = Crypt(buffer[i], num++);
        return num2;
    }
    
    public override void Write(byte[] buffer, int offset, int count)
    {
        var num = (uint)Stream.Position;
        var array = new byte[count];
        var num2 = 0U;
        while (num2 < (ulong)count)
        {
            array[num2] = Crypt(buffer[(int)(IntPtr)unchecked(num2 + (ulong)offset)], num + num2);
            num2++;
        }

        Stream.Write(array, 0, count);
    }


    public override void Flush()
    {
        Stream.Flush();
    }
    
    public override long Seek(long offset, SeekOrigin origin)
    {
        return Stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        Stream.SetLength(value);
    }
    
    public override void Close()
    {
        if (!LeaveOpen)
            Stream.Close();

        Stream = null;
        base.Close();
    }
    
    public override bool CanRead => Stream.CanRead;

    public override bool CanSeek => Stream.CanSeek;

    public override bool CanWrite => Stream.CanWrite;

    public override long Length => Stream.Length;

    public override long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }
}