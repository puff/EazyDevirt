using System.Reflection.Metadata;
using System.Text;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Devirtualization;
using Echo.Platforms.AsmResolver.Emulation;
using Echo.Platforms.AsmResolver.Emulation.Dispatch;
using Echo.Platforms.AsmResolver.Emulation.Stack;
using FieldDefinition = AsmResolver.DotNet.FieldDefinition;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;

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

    private static readonly CilCode[] AllowedCodes =
    {
        CilCode.Add, CilCode.Add_Ovf, CilCode.Add_Ovf_Un,
        CilCode.Sub, CilCode.Sub_Ovf, CilCode.Sub_Ovf_Un,
        CilCode.Mul, CilCode.Mul_Ovf, CilCode.Mul_Ovf_Un,

        CilCode.Div, CilCode.Div_Un, CilCode.Rem, CilCode.Rem_Un,
        CilCode.Xor, CilCode.And, CilCode.Or,

        CilCode.Shl, CilCode.Shr, CilCode.Shr_Un,
        CilCode.Ceq, CilCode.Cgt, CilCode.Cgt_Un, CilCode.Clt, CilCode.Clt_Un,

        CilCode.Conv_I8,
        CilCode.Ldelem_U1, CilCode.Ldfld
    };
    
    private byte[] _buffer = Array.Empty<byte>();
    
    // TODO: DOES NOT WORK
    public int ReadInt32Emulator()
    {
        var bytes = ReadBytes(4);
        
        var ctx = DevirtualizationContext.Instance;
        var vm = new CilVirtualMachine(ctx.Module, ctx.Module.IsBit32Required);
        
        var method = ctx.Module.LookupMember<MethodDefinition>(0x0600027F);
        
        var instanceType = method.DeclaringType!.ToTypeSignature();
        var isMemoryStreamField = ctx.Module.LookupMember<FieldDefinition>(0x040000C8);
        var bufferField = ctx.Module.LookupMember<FieldDefinition>(0x040000C1);
        
        var instance = vm.ValueFactory.CreateNativeInteger(vm.Heap.AllocateObject(instanceType, true));
        var instanceHandle = instance.AsObjectHandle(vm);

        // var isMemoryStream = vm.ValueFactory.BitVectorPool.Rent(8, false);
        // isMemoryStream.AsSpan().Write((byte)0); // if memorystream: 0 else: 1
        // instanceHandle.WriteField(isMemoryStreamField, isMemoryStream);
        
        var actualBuffer = vm.Heap.AllocateSzArray(ctx.Module.CorLibTypeFactory.Byte, bytes.Length, false);
        actualBuffer.AsObjectHandle(vm).WriteArrayData(bytes);
        var buffer = vm.ValueFactory.CreateNativeInteger(actualBuffer);
        buffer.AsSpan().WriteNativeInteger(actualBuffer, vm.Is32Bit);
        instanceHandle.WriteField(bufferField, buffer);
        
        var resultI32 = 0;
        vm.Dispatcher.BeforeInstructionDispatch += DispatcherOnBeforeInstructionDispatch;
        vm.CallStack.Returned += delegate(object? _, CallEventArgs args)
        {
            var result = args.Frame.EvaluationStack.Pop(ctx.Module.CorLibTypeFactory.Int32);
            resultI32 = result.AsSpan().I32;
        };

        // var result = vm.Call(method, new [] { instance });
        vm.CallStack.Push(method).WriteArgument(0, instance);
        vm.Run();

        return resultI32;

        return (bytes[2] << 8) | (bytes[3] << 16) | bytes[1] | (bytes[0] << 24);
    }

    private void DispatcherOnBeforeInstructionDispatch(object? _, CilDispatchEventArgs e)
    {
        var ins = e.Instruction;
        if (!AllowedCodes.Contains(ins.OpCode.Code))
            return;
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