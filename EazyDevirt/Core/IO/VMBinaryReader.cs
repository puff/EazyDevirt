using System.Text;

namespace EazyDevirt.Core.IO;

// TODO: The endianness is scrambled across samples. See issue #4
internal class VMBinaryReader : BinaryReader
{
    public override sbyte ReadSByte()
    {
        var bytes = ReadBytes(1);
        return (sbyte)bytes[0];
    }

    public override int ReadInt32()
    {
        var bytes = ReadBytes(4);
        return (bytes[3] << 8) | (bytes[0] << 16) | bytes[2] | (bytes[1] << 24);
    }
    
    /// <summary>
    /// Used in reading code instructions.
    /// </summary>
    /// <remarks>
    /// This is from a separate stream, called VMMemoryStream in the sample.
    /// The endianness of this is also scrambled across samples, see #4.
    /// </remarks>
    public int ReadInt32Special()
    {
        var bytes = ReadBytes(4);
        return (bytes[0] << 16) | (bytes[3] << 8) | bytes[2] | (bytes[1] << 24);
    }

    public override uint ReadUInt32()
    {
        var bytes = ReadBytes(4);
        return (uint)((bytes[3] << 24) | (bytes[0] << 16) | (bytes[2] << 8) | bytes[1]);
    }

    public override long ReadInt64()
    {
        var bytes = ReadBytes(8);
        return (long)((uint)(bytes[2] | (bytes[7] << 24) | (bytes[0] << 8) | (bytes[3] << 16)) | (ulong)((bytes[1] << 16) | bytes[4] | (bytes[5] << 24) | (bytes[6] << 8)) << 32);
    }

    public override ulong ReadUInt64()
    {
        var bytes = ReadBytes(8);
        return (uint)(bytes[3] | (bytes[0] << 24) | (bytes[7] << 16) | (bytes[2] << 8)) | (ulong)((bytes[5] << 16) | bytes[6] | (bytes[1] << 24) | (bytes[4] << 8)) << 32;
    }

    public override short ReadInt16()
    {
        var bytes = ReadBytes(2);
        return (short)((bytes[0] << 8) | bytes[1]);
    }
    
    public override ushort ReadUInt16()
    {
        var bytes = ReadBytes(2);
        return (ushort)((bytes[1] << 8) | bytes[0]);
    }

    public override float ReadSingle()
    {
        var bytes = ReadBytes(4);
        var array = new byte[4];
        array[0] = bytes[2];
        array[1] = bytes[1];
        array[2] = bytes[0];
        array[3] = bytes[3];
        
        using var reader = ToBinaryReader(array);
        return reader.ReadSingle();
    }

    public override double ReadDouble()
    {
        var bytes = ReadBytes(8);
        var array2 = new byte[8];
        array2[4] = bytes[0];
        array2[5] = bytes[1];
        array2[3] = bytes[5];
        array2[1] = bytes[7];
        array2[0] = bytes[6];
        array2[6] = bytes[4];
        array2[2] = bytes[2];
        array2[7] = bytes[3];
        
        using var reader = ToBinaryReader(array2);
        return reader.ReadDouble();
    }

    public override decimal ReadDecimal()
    {
        var bytes = ReadBytes(16);
        var array2 = new byte[16]; 
        array2[6] = bytes[9];
        array2[9] = bytes[14];
        array2[3] = bytes[0];
        array2[7] = bytes[7];
        array2[2] = bytes[5];
        array2[13] = bytes[3];
        array2[14] = bytes[2];
        array2[5] = bytes[1];
        array2[0] = bytes[4];
        array2[15] = bytes[10];
        array2[8] = bytes[13];
        array2[11] = bytes[12];
        array2[12] = bytes[8];
        array2[4] = bytes[15];
        array2[10] = bytes[11];
        array2[1] = bytes[6];

        using var reader = ToBinaryReader(array2);
        return reader.ReadDecimal();
    }

    private static BinaryReader ToBinaryReader(byte[] input)
    {
        var memoryStream = new MemoryStream(8);
        var binaryReader = new BinaryReader(memoryStream);
        binaryReader.BaseStream.Position = 0L;
        memoryStream.Write(input, 0, input.Length);
        memoryStream.Position = 0L;
        return binaryReader;
    }
    
    public VMBinaryReader(Stream input, bool leaveOpen = false) : base(input, Encoding.UTF8, leaveOpen)
    {
    }
}