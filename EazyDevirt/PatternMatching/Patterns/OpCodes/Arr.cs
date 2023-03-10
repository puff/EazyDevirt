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
