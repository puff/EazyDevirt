using EazyDevirt.Architecture;

namespace EazyDevirt.Core.IO;

/// <summary>
/// Big-endian binary reader
/// </summary>
internal class VMBinaryReader : BinaryReader
{
    private byte[] buffer = new byte[16];
    
    public VMBinaryReader(Stream input) : base(input, new VMEncoding())
    {
        
    }

    private void FillBuffer(int int_1)
    {
        int num = 0;
        int num2;
        if (int_1 != 1)
        {
            for (;;)
            {
                num2 = BaseStream.Read(buffer, num, int_1 - num);
                if (num2 == 0)
                {
                    break;
                }
                num += num2;
                if (num >= int_1)
                {
                    return;
                }
            }
            throw new Exception();
        }
        num2 = BaseStream.ReadByte();
        if (num2 == -1)
        {
            throw new Exception();
        }
        buffer[0] = (byte)num2;
    }

    public override int ReadInt32()
    {
        FillBuffer(4);
        return (buffer[2] << 8) | (buffer[3] << 16) | buffer[1] | (buffer[0] << 24);
    }
}