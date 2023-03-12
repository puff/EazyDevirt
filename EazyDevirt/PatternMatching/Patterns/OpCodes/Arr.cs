using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

internal record Newarr : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_1,     // 46	0071	ldloc.1
        CilOpCodes.Call,        // 47	0072	call	class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type, int32)
        CilOpCodes.Stloc_S,     // 48	0077	stloc.s	V_6 (6)
        CilOpCodes.Ldarg_0,     // 49	0079	ldarg.0
        CilOpCodes.Newobj,      // 50	007A	newobj	instance void VMArray::.ctor()
        CilOpCodes.Dup,         // 51	007F	dup
        CilOpCodes.Ldloc_S,     // 52	0080	ldloc.s	V_6 (6)
        CilOpCodes.Callvirt,    // 53	0082	callvirt	instance void VMArray::method_4(class [mscorlib]System.Array)
        CilOpCodes.Callvirt,    // 54	0087	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 55	008C	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Newarr;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 1].Operand is SerializedMemberReference
        {
            FullName: "System.Array System.Array::CreateInstance(System.Type, System.Int32)"
        };
}

internal record Ldlen : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Castclass,   // 3	000B	castclass	[mscorlib]System.Array
        CilOpCodes.Stloc_0,     // 4	0010	stloc.0
        CilOpCodes.Ldarg_0,     // 5	0011	ldarg.0
        CilOpCodes.Ldloc_0,     // 6	0012	ldloc.0
        CilOpCodes.Callvirt,    // 7	0013	callvirt	instance int32 [mscorlib]System.Array::get_Length()
        CilOpCodes.Newobj,      // 8	0018	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Callvirt,    // 9	001D	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 10	0022	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldlen;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 7].Operand is SerializedMemberReference
        {
            FullName: "System.Int32 System.Array::get_Length()"
        };
}

