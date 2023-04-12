using System.Text;
using AsmResolver.DotNet;
using EazyDevirt.Core.Abstractions;

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
}