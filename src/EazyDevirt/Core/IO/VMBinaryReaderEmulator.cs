using System.Text;
using AsmResolver.DotNet;
using EazyDevirt.Core.Abstractions.IO;

namespace EazyDevirt.Core.IO;

// BIG thank you to void-stack for this!
internal class VMBinaryReaderEmulator : VMBinaryReaderBase
{
    private readonly BinaryEndiannessEmulator _emulator;

    public VMBinaryReaderEmulator(Stream input, BinaryEndiannessEmulator emulator, bool leaveOpen = false)
        : base(input, Encoding.UTF8, leaveOpen)
    {
        _emulator = emulator;
    }

    public override sbyte ReadSByte()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint)0x06000271);
        var bytes = ReadBytes(1);

        return _emulator.InferScrambledEndianness<sbyte>(method, bytes);
    }

    public override int ReadInt32()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint)0x0600027F);
        var bytes = ReadBytes(4);

        return _emulator.InferScrambledEndianness<int>(method, bytes);
    }

    public override int ReadInt32Special()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint)0x0600003E);

        var bytes = ReadBytes(4);
        return _emulator.InferScrambledEndianness<int>(method, bytes, this);
    }
    
    public override uint ReadUInt32()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint)0x06000280);
        var bytes = ReadBytes(4);

        return _emulator.InferScrambledEndianness<uint>(method, bytes);
    }
    
    public override long ReadInt64()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint)0x06000281);
        var bytes = ReadBytes(8);

        return _emulator.InferScrambledEndianness<long>(method, bytes);
    }
    
    public override ulong ReadUInt64()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint)0x06000282);
        var bytes = ReadBytes(8);

        return _emulator.InferScrambledEndianness<ulong>(method, bytes);
    }
    
    public override short ReadInt16()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint)0x06000283);
        var bytes = ReadBytes(2);

        return _emulator.InferScrambledEndianness<short>(method, bytes);
    }
    
    public override ushort ReadUInt16()
    {
        var method = _emulator.Module.LookupMember<MethodDefinition>((uint) 0x06000284 );
        var bytes = ReadBytes(2);

        return _emulator.InferScrambledEndianness<ushort>(method, bytes);
    }
}