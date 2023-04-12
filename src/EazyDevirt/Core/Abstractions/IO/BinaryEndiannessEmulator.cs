using System.Collections;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Devirtualization;
using Echo.DataFlow;
using Echo.DataFlow.Analysis;
using Echo.Memory;
using Echo.Platforms.AsmResolver;
using Echo.Platforms.AsmResolver.Emulation;
using Echo.Platforms.AsmResolver.Emulation.Dispatch;
using Echo.Platforms.AsmResolver.Emulation.Stack;

namespace EazyDevirt.Core.Abstractions.IO;

// BIG thank you to void-stack for this!
public class BinaryEndiannessEmulator
{
    public static BinaryEndiannessEmulator Instance
    {
        get;
    } = new(DevirtualizationContext.Instance.Module);
    
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

    private readonly CilExecutionContext _context;
    private readonly CilVirtualMachine _vm;

    public BinaryEndiannessEmulator(ModuleDefinition module)
    {
        Module = module;
        _vm = new CilVirtualMachine(module, module.IsBit32Required);
        _context = new CilExecutionContext(_vm, CancellationToken.None);
    }

    public ModuleDefinition Module { get; }

    public T? InferScrambledEndianness<T>(MethodDefinition method, IEnumerable<byte> bytes, BinaryReader? reader = null!)
    {
        if (method.CilMethodBody is null)
            throw new ArgumentException("Method has no body", nameof(method));

        var instructions = method.CilMethodBody.Instructions;
        instructions.CalculateOffsets();

        method.CilMethodBody.ConstructSymbolicFlowGraph(out var dfg);
        var scrambledEndianness =
            _vm.ObjectMarshaller.ToObject(GetInitialEndianness(instructions, dfg, bytes, reader).AsSpan(),
                typeof(T));

        if (scrambledEndianness is not null)
            return (T)scrambledEndianness;

        return default;
    }

    private BitVector GetInitialEndianness(CilInstructionCollection instructions,
        DataFlowGraph<CilInstruction> dfg, IEnumerable<byte> bytes, BinaryReader? reader = null!)
    {
        var useReader = reader is not null;
        var lastInstruction = instructions.Last();
        var node = dfg.Nodes[lastInstruction.Offset];

        _vm.CallStack.Push(instructions.Owner.Owner);

        var factory = _vm.ContextModule.CorLibTypeFactory;
        var enumerable = bytes as byte[] ?? bytes.ToArray();
        var eventHandler = SpoofBufferField(enumerable, factory);
        _vm.Dispatcher.BeforeInstructionDispatch += eventHandler;

        var frame = _context.CurrentFrame;
        var stack = frame.EvaluationStack;

        if (useReader)
        {
            // write buffer to trick into (this._position += 4);
            var writeBuffer = _vm.ObjectMarshaller.ToBitVector(4);
            frame.WriteLocal(1, writeBuffer);
        }

        foreach (var dep in node.GetOrderedDependencies(DependencyCollectionFlags.IncludeStackDependencies))
        {
            var contents = dep.Contents;

            if (AllowedCodes.Contains(contents.OpCode.Code) || contents.IsLdcI4() || (contents.IsLdloc() && useReader))
                _vm.Dispatcher.Dispatch(_context, contents);
        }

        _vm.Dispatcher.BeforeInstructionDispatch -= eventHandler;
        return stack.Pop().Contents;
    }

    private static EventHandler<CilDispatchEventArgs>? SpoofBufferField(IEnumerable<byte> bytes, CorLibTypeFactory factory)
    {
        return delegate(object? sender, CilDispatchEventArgs args)
        {
            switch (args.Instruction.OpCode.Code)
            {
                /* Fake the Ldfld of _buffer with our buffer */
                case CilCode.Ldfld:
                {
                    var comparer = SignatureComparer.Default;

                    if (args.Instruction.Operand is FieldDefinition { Signature: not null } field &&
                        comparer.Equals(field.Signature.FieldType, factory.Byte.MakeSzArrayType()))
                    {
                        args.IsHandled = true;

                        var evalStack = args.Context.CurrentFrame.EvaluationStack;
                        var valueFactory = args.Context.Machine.ValueFactory;
                        var marshaller = args.Context.Machine.ObjectMarshaller;

                        var result = valueFactory.CreateValue(field.Signature.FieldType, false);
                        result.AsSpan().Write(marshaller.ToBitVector(bytes));
                        evalStack.Push(result, field.Signature.FieldType);
                    }

                    break;
                }
                case CilCode.Ldelem_U1:
                {
                    args.IsHandled = true;

                    var evalStack = args.Context.CurrentFrame.EvaluationStack;
                    var valueFactory = args.Context.Machine.ValueFactory;

                    var index = evalStack.Pop().Contents.AsSpan().I32;
                    var value = bytes.ToArray()[index];
                    var result = valueFactory.BitVectorPool.Rent(8, false);
                    
                    result.AsSpan().Write(value);
                    evalStack.Push(result, factory.Byte.Type.ToTypeSignature());
                    break;
                }
                default:
                    return;
            }
        };
    }
}