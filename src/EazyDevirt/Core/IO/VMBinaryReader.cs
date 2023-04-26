using System.Text;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Devirtualization;
using Echo.Memory;
using Echo.Platforms.AsmResolver.Emulation;
using Echo.Platforms.AsmResolver.Emulation.Dispatch;

namespace EazyDevirt.Core.IO;

internal class VMBinaryReader : VMBinaryReaderBase
{
    private static readonly DevirtualizationContext Ctx = DevirtualizationContext.Instance;
    
    private static ITypeDefOrRef _instanceType = null!;
    private static FieldDefinition _isMemoryStreamFieldDef = null!;
    private static FieldDefinition _bufferFieldDef = null!;

    private static MethodDefinition _readInt32MethodDef = null!;
    private static MethodDefinition _readUInt32MethodDef = null!;
    private static MethodDefinition _readInt64MethodDef = null!;
    private static MethodDefinition _readUInt64MethodDef = null!;
    private static MethodDefinition _readInt16MethodDef = null!;
    private static MethodDefinition _readUInt16MethodDef = null!;
    
    private readonly CilVirtualMachine _vm;
    private BitVector InstanceObj;
    
    public VMBinaryReader(Stream input, bool leaveOpen = false) : base(input, Encoding.UTF8, leaveOpen)
    {
        _vm = new CilVirtualMachine(Ctx.Module, Ctx.Module.IsBit32Required);
        _vm.Dispatcher.BeforeInstructionDispatch += DispatcherOnBeforeInstructionDispatch;
        
        FindInstanceDefs();
        if (_instanceType is null)
            throw new ArgumentNullException(nameof(_instanceType), "Failed finding VMBinaryReader instance type!");
        if (_isMemoryStreamFieldDef is null)
            throw new ArgumentNullException(nameof(_isMemoryStreamFieldDef), "Failed finding VMBinaryReader _isMemoryStream field!");
        if (_bufferFieldDef is null)
            throw new ArgumentNullException(nameof(_bufferFieldDef), "Failed finding VMBinaryReader _buffer field!");
        
        SetupInstanceObj();
    }

    private static void FindInstanceDefs()
    {
        _readInt32MethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x0600027F);
        _readUInt32MethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000280);
        _readInt64MethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000281);
        _readUInt64MethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000282);
        _readInt16MethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000283);
        _readUInt16MethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000284);

        _instanceType = _readInt32MethodDef.DeclaringType!;
        _isMemoryStreamFieldDef = Ctx.Module.LookupMember<FieldDefinition>(0x040000C8);
        _bufferFieldDef = Ctx.Module.LookupMember<FieldDefinition>(0x040000C1);
    }

    private void SetupInstanceObj()
    {
        var instanceAddress = _vm.Heap.AllocateObject(_instanceType, true); 
        var instanceObjectSpan = _vm.Heap.GetObjectSpan(instanceAddress);
        var isMemoryStreamField = instanceObjectSpan.SliceObjectField(_vm.ValueFactory, _isMemoryStreamFieldDef);
        isMemoryStreamField.U8 = 0; // this should never have to be set to 1 unless ReadInt32Special changes endianness.
        InstanceObj = _vm.ValueFactory.CreateNativeInteger(instanceAddress);
    }
    
    private static void DispatcherOnBeforeInstructionDispatch(object? _, CilDispatchEventArgs e)
    {
        var ins = e.Instruction;
        var frame = e.Context.CurrentFrame;
        var factory = e.Context.Machine.ValueFactory;
        switch (ins.OpCode.Code)
        {
            case CilCode.Ldarg_0:
                var instanceTypeSig = _instanceType.ToTypeSignature();
                var arg = factory.RentValue(instanceTypeSig, false);
                frame.ReadArgument(0, arg.AsSpan());
                frame.EvaluationStack.Push(arg, instanceTypeSig);
                break;
            default:
                return;
        }

        e.IsHandled = true;
        frame.ProgramCounter += ins.Size;
    }

    private BitVector ReadEmulated<T>(byte[] bytes)
    {
        var instanceObjectSpan = _vm.Heap.GetObjectSpan(InstanceObj.AsSpan().I64);
        
        var bufferField = instanceObjectSpan.SliceObjectField(_vm.ValueFactory, _bufferFieldDef);
        bufferField.Write(_vm.ObjectMarshaller.ToBitVector(bytes));

        _vm.CallStack.Peek().WriteArgument(0, InstanceObj);
        _vm.Run();
        
        var typeSig = Ctx.Module.DefaultImporter.ImportTypeSignature(typeof(T));
        return _vm.CallStack.Peek().EvaluationStack.Pop(typeSig);
    }

    public override sbyte ReadSByte()
    {
        var bytes = ReadBytes(1);
        return (sbyte)bytes[0];
    }

    public override int ReadInt32()
    {
        var bytes = ReadBytes(4);

        _vm.CallStack.Push(_readInt32MethodDef);
        return ReadEmulated<int>(bytes).AsSpan().I32;
        // return (bytes[2] << 8) | (bytes[3] << 16) | bytes[1] | (bytes[0] << 24);
    }

    // this always has the same endianness as ReadInt32 in the samples i've seen
    public override int ReadInt32Special()
    {
        var bytes = ReadBytes(4);
        
        _vm.CallStack.Push(_readInt32MethodDef);
        return ReadEmulated<int>(bytes).AsSpan().I32;
        // return (bytes[3] << 16) | (bytes[2] << 8) | bytes[1] | (bytes[0] << 24);
    }

    public override uint ReadUInt32()
    {
        var bytes = ReadBytes(4);
        
        _vm.CallStack.Push(_readUInt32MethodDef);
        return ReadEmulated<uint>(bytes).AsSpan().U32;
        // return (uint)((bytes[0] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[3]);
    }

    public override long ReadInt64()
    {
        var bytes = ReadBytes(8);
                
        _vm.CallStack.Push(_readInt64MethodDef);
        return ReadEmulated<long>(bytes).AsSpan().I64;
        // return (long)((uint)((bytes[0] << 16) | (bytes[1] << 24) | bytes[4] | (bytes[2] << 8)) | (ulong)(bytes[6] | (bytes[3] << 16) | (bytes[5] << 8) | (bytes[7] << 24)) << 32);
    }

    public override ulong ReadUInt64()
    {
        var bytes = ReadBytes(8);
                
        _vm.CallStack.Push(_readUInt64MethodDef);
        return ReadEmulated<ulong>(bytes).AsSpan().U64;
        // return (uint)((bytes[6] << 16) | (bytes[0] << 24) | bytes[7] | (bytes[4] << 8)) | (ulong)((bytes[2] << 24) | (bytes[1] << 8) | (bytes[5] << 16) | bytes[3]) << 32;
    }

    public override short ReadInt16()
    {
        var bytes = ReadBytes(2);
                
        _vm.CallStack.Push(_readInt16MethodDef);
        return ReadEmulated<short>(bytes).AsSpan().I16;
        // return (short)((bytes[1] << 8) | bytes[0]);
    }
    
    public override ushort ReadUInt16()
    {
        var bytes = ReadBytes(2);
                
        _vm.CallStack.Push(_readUInt16MethodDef);
        return ReadEmulated<ushort>(bytes).AsSpan().U16;
        // return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    // TODO: Emulate ReadSingle, ReadDouble, ReadDecimal
    
    public override float ReadSingle()
    {
        var bytes = ReadBytes(4);
        var array = new byte[4];
        array[0] = bytes[3];
        array[1] = bytes[1];
        array[2] = bytes[0];
        array[3] = bytes[2];
        
        using var reader = ToBinaryReader(array);
        return reader.ReadSingle();
    }

    public override double ReadDouble()
    {
        var bytes = ReadBytes(8);
        var array2 = new byte[8];
        array2[0] = bytes[5];
        array2[5] = bytes[4];
        array2[6] = bytes[2];
        array2[7] = bytes[3];
        array2[3] = bytes[0];
        array2[4] = bytes[1];
        array2[1] = bytes[6];
        array2[2] = bytes[7];
        
        using var reader = ToBinaryReader(array2);
        return reader.ReadDouble();
    }

    public override decimal ReadDecimal()
    {
        var bytes = ReadBytes(16);
        var array2 = new byte[16]; 
        array2[6] = bytes[5];
        array2[12] = bytes[9];
        array2[1] = bytes[7];
        array2[3] = bytes[4];
        array2[11] = bytes[11];
        array2[9] = bytes[1];
        array2[7] = bytes[2];
        array2[4] = bytes[0];
        array2[13] = bytes[8];
        array2[10] = bytes[15];
        array2[15] = bytes[14];
        array2[2] = bytes[12];
        array2[8] = bytes[13];
        array2[5] = bytes[6];
        array2[0] = bytes[3];
        array2[14] = bytes[10];

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
}