using System.Diagnostics.CodeAnalysis;
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
    
    private static TypeDefinition _instanceType = null!;
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
        _vm = new CilVirtualMachine(Ctx.Module, false); // 32 bit always breaks something, even on 32 bit only assemblies.
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
    
    #region Setup

    // overly complicated method to find VMBinaryReader stuff
    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    private static void FindInstanceDefs()
    {
        foreach(var type in Ctx.Module.GetAllTypes())
        {
            var fields = type.Fields;

            try
            {
                // private MemoryStream memoryStream_0;
                if (fields.SingleOrDefault(x => x is
                        { IsStatic: false, IsPrivate: true, Signature.FieldType.FullName: "System.IO.MemoryStream" }) is null)
                    continue;
                
                // private BinaryReader binaryReader_0;
                if (fields.SingleOrDefault(x => x is
                        { IsStatic: false, IsPrivate: true, Signature.FieldType.FullName: "System.IO.BinaryReader" }) is null)
                    continue;
                
                // private Decoder m_decoder;
                if (fields.SingleOrDefault(x => x is
                        { IsStatic: false, IsPrivate: true, Signature.FieldType.FullName: "System.Text.Decoder" }) is null)
                    continue;
                
                var methods = type.Methods;

                // internal static decimal ToDecimal(byte[] byte_3)
                var toDecimalInternal = methods.SingleOrDefault(x => x is
                    { IsStatic: true, IsAssembly: true, Signature.ReturnType.FullName: "System.Decimal" });
                if (toDecimalInternal is null)
                    continue;
                
                // private static decimal ToDecimal(int int_1, int int_2, int int_3, int int_4)
                var toDecimalPrivate = methods.SingleOrDefault(x => x is
                    { IsStatic: true, IsPrivate: true, Signature.ReturnType.FullName: "System.Decimal" });
                if (toDecimalPrivate is null)
                    continue;

                _instanceType = type;
                break;
            }
            catch (InvalidOperationException)
            {
            }
        }

        if (_instanceType is null)
            return;

        foreach (var method in _instanceType.Methods)
        {
            var sig = method.Signature;
            if (sig is null or { ReturnsValue: false } || sig.ReturnType.FullName != "System.Int32") // for ReadInt32
                continue;
            
            var instructions = method.CilMethodBody?.Instructions;
            if (instructions is null)
                continue;

            try
            {
                var ldflds = instructions.Where(x => x.OpCode.Code is CilCode.Ldfld).ToArray();
                if (ldflds.Length == 6)
                {
                    _isMemoryStreamFieldDef = (ldflds.SingleOrDefault(x => x.Operand is FieldDefinition
                    {
                        Signature.FieldType.FullName: "System.Boolean"
                    })?.Operand as FieldDefinition)!;
                    if (_isMemoryStreamFieldDef is null)
                        continue;
                    
                    var buffer = ldflds.FirstOrDefault(x => x.Operand is FieldDefinition
                    {
                        Signature.FieldType.FullName: "System.Byte[]"
                    } xf && ldflds.Count(l => l.Operand is IFieldDescriptor f && f.FullName == xf.FullName) == 4);
                    
                    _bufferFieldDef = (buffer?.Operand as FieldDefinition)!;
                    if (_bufferFieldDef is null)
                        continue;

                    var mdtoken = method.MetadataToken.ToUInt32();
                    // the order of these is the same across every sample that has been tested
                    _readInt32MethodDef = method;
                    _readUInt32MethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken += 1);
                    _readInt64MethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken += 1);
                    _readUInt64MethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken += 1);
                    _readInt16MethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken += 1);
                    _readUInt16MethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken += 1);
                    _readSingleMethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken += 2); // skip 1
                    _readDoubleMethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken += 1);
                    _readDecimalMethodDef = Ctx.Module.LookupMember<MethodDefinition>(mdtoken + 1);

                    if (_readDecimalMethodDef.Signature is not
                        { ReturnsValue: true, ReturnType.FullName: "System.Decimal" } && Ctx.Options.VeryVerbose)
                    {
                        Ctx.Console.Warning($"{nameof(_readDecimalMethodDef)} does not have correct return type on current iteration. VMBinaryReader may not be initialized correctly.");
                        continue;
                    }
                    
                    break;
                }
                
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void SetupInstanceObj()
    {
        var instanceAddress = _vm.Heap.AllocateObject(_instanceType, true); 
        var instanceObjectSpan = _vm.Heap.GetObjectSpan(instanceAddress);
        var isMemoryStreamField = instanceObjectSpan.SliceObjectField(_vm.ValueFactory, _isMemoryStreamFieldDef);
        isMemoryStreamField.U8 = 0; // this should never have to be set to 1 unless ReadInt32Special changes endianness.
        InstanceObj = _vm.ValueFactory.CreateNativeInteger(instanceAddress);
    }
    
    #endregion Setup

    #region Emulation
    
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
        var instanceObjectSpan = _vm.Heap.GetObjectSpan(InstanceObj);
        
        var bufferField = instanceObjectSpan.SliceObjectField(_vm.ValueFactory, _bufferFieldDef);
        bufferField.Write(_vm.ObjectMarshaller.ToBitVector(bytes));

        _vm.CallStack.Peek().WriteArgument(0, InstanceObj);
        _vm.Run();
        
        var typeSig = Ctx.Importer.ImportTypeSignature(typeof(T));
        return _vm.ObjectMarshaller.ToObject<T>(_vm.CallStack.Peek().EvaluationStack.Pop(typeSig))!;
    }
    
    #endregion Emulation

    #region Overrides
    
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
    
    #endregion Overrides

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