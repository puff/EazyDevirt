using System.Diagnostics;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Devirtualization;
using Echo.Platforms.AsmResolver.Emulation;
using Echo.Platforms.AsmResolver.Emulation.Dispatch;

namespace EazyDevirt.Core.IO;

internal class VMBinaryReader : VMBinaryReaderBase
{
    public VMBinaryReader(Stream input, bool leaveOpen = false) : base(input, Encoding.UTF8, leaveOpen)
    {
    }
    
    public override sbyte ReadSByte()
    {
        var bytes = ReadBytes(1);
        return (sbyte)bytes[0];
    }

    public override int ReadInt32()
    {
        var bytes = ReadBytes(4);
        return (bytes[2] << 8) | (bytes[3] << 16) | bytes[1] | (bytes[0] << 24);
    }

    private ITypeDefOrRef InstanceType;
    
    public int ReadInt32Emulator()
    {
        var bytes = ReadBytes(4);
        
        var ctx = DevirtualizationContext.Instance;
        var vm = new CilVirtualMachine(ctx.Module, ctx.Module.IsBit32Required);

        var method = ctx.Module.LookupMember<MethodDefinition>(0x0600027F);

        InstanceType = method.DeclaringType!;
        var isMemoryStreamFieldDef = ctx.Module.LookupMember<FieldDefinition>(0x040000C8);
        var bufferFieldDef = ctx.Module.LookupMember<FieldDefinition>(0x040000C1);

        var instanceAddress = vm.Heap.AllocateObject(InstanceType, true);
        var instance = vm.ValueFactory.CreateNativeInteger(instanceAddress);
        var instanceObjectSpan = vm.Heap.GetObjectSpan(instanceAddress);
        var instanceDataSpan = instanceObjectSpan.SliceObjectData(vm.ValueFactory);

        var isMemoryStreamField = instanceDataSpan.SliceStructField(vm.ValueFactory, isMemoryStreamFieldDef);
        isMemoryStreamField.U8 = 0; // if memorystream: 1 else: 0

        var bufferField = instanceDataSpan.SliceStructField(vm.ValueFactory, bufferFieldDef);
        bufferField.Write(vm.ObjectMarshaller.ToBitVector(bytes));
        
        vm.Dispatcher.BeforeInstructionDispatch += DispatcherOnBeforeInstructionDispatch;

        // var result = vm.Call(method, new [] { instance });
        vm.CallStack.Push(method).WriteArgument(0, instance);
        vm.Run();

        var result = vm.CallStack.Peek().EvaluationStack.Pop(ctx.Module.CorLibTypeFactory.Int32);
        var resultI32 = result.AsSpan().I32;
        if (Debugger.IsAttached)
            Debug.Assert(resultI32 == ((bytes[2] << 8) | (bytes[3] << 16) | bytes[1] | (bytes[0] << 24)));
        return resultI32;
    }

    private void DispatcherOnBeforeInstructionDispatch(object? _, CilDispatchEventArgs e)
    {
        var ins = e.Instruction;
        var frame = e.Context.CurrentFrame;
        var factory = e.Context.Machine.ValueFactory;
        switch (ins.OpCode.Code)
        {
            case CilCode.Ldarg_0:
                var instanceTypeSig = InstanceType.ToTypeSignature();
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
    
    public override int ReadInt32Special()
    {
        var bytes = ReadBytes(4);
        return (bytes[3] << 16) | (bytes[2] << 8) | bytes[1] | (bytes[0] << 24);
    }

    public override uint ReadUInt32()
    {
        var bytes = ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[3]);
    }

    public override long ReadInt64()
    {
        var bytes = ReadBytes(8);
        return (long)((uint)((bytes[0] << 16) | (bytes[1] << 24) | bytes[4] | (bytes[2] << 8)) | (ulong)(bytes[6] | (bytes[3] << 16) | (bytes[5] << 8) | (bytes[7] << 24)) << 32);
    }

    public override ulong ReadUInt64()
    {
        var bytes = ReadBytes(8);
        return (uint)((bytes[6] << 16) | (bytes[0] << 24) | bytes[7] | (bytes[4] << 8)) | (ulong)((bytes[2] << 24) | (bytes[1] << 8) | (bytes[5] << 16) | bytes[3]) << 32;
    }

    public override short ReadInt16()
    {
        var bytes = ReadBytes(2);
        return (short)((bytes[1] << 8) | bytes[0]);
    }
    
    public override ushort ReadUInt16()
    {
        var bytes = ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

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