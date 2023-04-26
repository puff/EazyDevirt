using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Devirtualization;
using Echo.Memory;
using Echo.Platforms.AsmResolver.Emulation;
using Echo.Platforms.AsmResolver.Emulation.Dispatch;
using Echo.Platforms.AsmResolver.Emulation.Stack;

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
    private static MethodDefinition _readSingleMethodDef = null!;
    private static MethodDefinition _readDoubleMethodDef = null!;
    private static MethodDefinition _readDecimalMethodDef = null!;
    
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
        _readSingleMethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000286);
        _readDoubleMethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000287);
        _readDecimalMethodDef = Ctx.Module.LookupMember<MethodDefinition>(0x06000288);

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
        var vm = e.Context.Machine;
        var factory = vm.ValueFactory;
        switch (ins.OpCode.Code)
        {
            case CilCode.Ldarg_0:
                var instanceTypeSig = _instanceType.ToTypeSignature();
                var arg = factory.RentValue(instanceTypeSig, false);
                frame.ReadArgument(0, arg.AsSpan());
                frame.EvaluationStack.Push(arg, instanceTypeSig);
                break;
            
            case CilCode.Call:
                var method = (ins.Operand as IMethodDescriptor)!;
                if (method.Signature is { ReturnsValue: true })
                {
                    if (method.DeclaringType!.FullName == "System.IO.BinaryReader")
                    {
                        switch (method.Signature.ReturnType.FullName)
                        {
                            // this.ToBinaryReader(array2).ReadSingle();
                            case "System.Single":
                                var readerF = vm.ObjectMarshaller.ToObject<BinaryReader>(
                                    frame.EvaluationStack.Pop(
                                        Ctx.Importer.ImportTypeSignature(typeof(BinaryReader))))!;
                                frame.EvaluationStack.Push(new StackSlot(
                                    vm.ObjectMarshaller.ToBitVector(readerF.ReadSingle()), StackSlotTypeHint.Float));
                                break;
                            
                            // this.ToBinaryReader(array2).ReadDouble();
                            case "System.Double":
                                var readerD = vm.ObjectMarshaller.ToObject<BinaryReader>(
                                    frame.EvaluationStack.Pop(
                                        Ctx.Importer.ImportTypeSignature(typeof(BinaryReader))))!;
                                frame.EvaluationStack.Push(new StackSlot(
                                    vm.ObjectMarshaller.ToBitVector(readerD.ReadDouble()), StackSlotTypeHint.Float));
                                break;
                        }
                    }
                    else
                    {
                        switch (method.Signature.ReturnType.FullName)
                        {
                            // byte[] array2 = this.method_25();
                            case "System.Byte[]":
                                var byteArray = vm.ObjectMarshaller.ToBitVector(new byte[16]);
                                frame.EvaluationStack.Push(byteArray, new SzArrayTypeSignature(Ctx.Module.CorLibTypeFactory.Byte));
                                break;
                            
                            // this.ToBinaryReader(array2)
                            case "System.IO.BinaryReader":
                                var bytes = vm.ObjectMarshaller.ToObject<byte[]>(
                                    frame.EvaluationStack.Pop(
                                        new SzArrayTypeSignature(Ctx.Module.CorLibTypeFactory.Byte)))!;
                                var reader = ToBinaryReader(bytes);
                                var readerMarshalled = vm.ObjectMarshaller.ToBitVector(reader);
                                frame.EvaluationStack.Push(readerMarshalled,
                                    Ctx.Importer.ImportTypeSignature(typeof(BinaryReader)));
                                break;
                        }
                    }
                }

                break;

            default:
                return;
        }

        e.IsHandled = true;
        frame.ProgramCounter += ins.Size;
    }

    private T ReadEmulated<T>(byte[] bytes)
    {
        var instanceObjectSpan = _vm.Heap.GetObjectSpan(InstanceObj.AsSpan().I64);
        
        var bufferField = instanceObjectSpan.SliceObjectField(_vm.ValueFactory, _bufferFieldDef);
        bufferField.Write(_vm.ObjectMarshaller.ToBitVector(bytes));

        _vm.CallStack.Peek().WriteArgument(0, InstanceObj);
        _vm.Run();
        
        var typeSig = Ctx.Importer.ImportTypeSignature(typeof(T));
        return _vm.ObjectMarshaller.ToObject<T>(_vm.CallStack.Peek().EvaluationStack.Pop(typeSig))!;
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
        return ReadEmulated<int>(bytes);
    }

    // this always has the same endianness as ReadInt32 in the samples i've seen
    public override int ReadInt32Special()
    {
        var bytes = ReadBytes(4);
        
        _vm.CallStack.Push(_readInt32MethodDef);
        return ReadEmulated<int>(bytes);
    }

    public override uint ReadUInt32()
    {
        var bytes = ReadBytes(4);
        
        _vm.CallStack.Push(_readUInt32MethodDef);
        return ReadEmulated<uint>(bytes);
    }

    public override long ReadInt64()
    {
        var bytes = ReadBytes(8);
                
        _vm.CallStack.Push(_readInt64MethodDef);
        return ReadEmulated<long>(bytes);
    }

    public override ulong ReadUInt64()
    {
        var bytes = ReadBytes(8);
                
        _vm.CallStack.Push(_readUInt64MethodDef);
        return ReadEmulated<ulong>(bytes);
    }

    public override short ReadInt16()
    {
        var bytes = ReadBytes(2);
                
        _vm.CallStack.Push(_readInt16MethodDef);
        return ReadEmulated<short>(bytes);
    }
    
    public override ushort ReadUInt16()
    {
        var bytes = ReadBytes(2);
                
        _vm.CallStack.Push(_readUInt16MethodDef);
        return ReadEmulated<ushort>(bytes);
    }
    
    // TODO: Emulated ReadSingle, ReadDouble, ReadDecimal are untested
    
    public override float ReadSingle()
    {
        var bytes = ReadBytes(4);
        
        _vm.CallStack.Push(_readSingleMethodDef);
        return ReadEmulated<float>(bytes);
    }

    public override double ReadDouble()
    {
        var bytes = ReadBytes(8);
        
        _vm.CallStack.Push(_readDoubleMethodDef);
        return ReadEmulated<double>(bytes);
    }

    public override decimal ReadDecimal()
    {
        var bytes = ReadBytes(16);

        _vm.CallStack.Push(_readDecimalMethodDef);
        return ReadEmulated<decimal>(bytes);
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