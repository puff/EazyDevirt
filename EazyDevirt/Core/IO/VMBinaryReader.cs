using EazyDevirt.Architecture;

namespace EazyDevirt.Core.IO;

/// <summary>
/// Big-endian binary reader.
/// </summary>
/// <remarks>
/// Mostly copied from decompilation.
/// </remarks>
internal class VMBinaryReader
{
    private CryptoStreamV2 _stream;
    private byte[] _buffer = new byte[16];

    public void SetMethodKey(long methodKey)
    {
        _stream.CurrentMethodKey = methodKey;
    }
    
    private void FillBuffer(int int1)
    {
        var num = 0;
        int num2;
        if (int1 != 1)
        {
            for (;;)
            {
                num2 = _stream.Read(_buffer, num, int1 - num);
                if (num2 == 0)
                    break;
                num += num2;
                if (num >= int1)
                    return;
            }
            throw new Exception();
        }
        num2 = _stream.ReadByte();
        if (num2 == -1)
            throw new Exception();
        
        _buffer[0] = (byte)num2;
    }

    public byte ReadByte()
    {
        var num = _stream.ReadByte();
        if (num == -1)
            throw new Exception("ReadByte: num == -1");

        return (byte)num;
    }

    public sbyte ReadSByte()
    {
        FillBuffer(1);
        return (sbyte)_buffer[0];
    }

    public int ReadInt32()
    {
        FillBuffer(4);
        return (_buffer[2] << 8) | (_buffer[3] << 16) | _buffer[1] | (_buffer[0] << 24);
    }

    public uint ReadUInt32()
    {
        FillBuffer(4);
        return (uint)((_buffer[0] << 24) | (_buffer[2] << 16) | (_buffer[1] << 8) | _buffer[3]);
    }

    public long ReadInt64()
    {
        FillBuffer(8);
        return (long)((uint)((_buffer[0] << 16) | (_buffer[1] << 24) | _buffer[4] | (_buffer[2] << 8)) | (ulong)(_buffer[6] | (_buffer[3] << 16) | (_buffer[5] << 8) | (_buffer[7] << 24)) << 32);
    }

    public ulong ReadUInt64()
    {
        FillBuffer(8);
        return (uint)((_buffer[6] << 16) | (_buffer[0] << 24) | _buffer[7] | (_buffer[4] << 8)) | (ulong)((_buffer[2] << 24) | (_buffer[1] << 8) | (_buffer[5] << 16) | _buffer[3]) << 32;
    }

    public short ReadInt16()
    {
        FillBuffer(2);
        return (short)((_buffer[1] << 8) | _buffer[0]);
    }

    public ushort ReadUInt16()
    {
        FillBuffer(2);
        return (ushort)((_buffer[0] << 8) | _buffer[1]);
    }
    
    public VMBinaryReader(CryptoStreamV2 input)
    {
        _stream = input;
    }

}