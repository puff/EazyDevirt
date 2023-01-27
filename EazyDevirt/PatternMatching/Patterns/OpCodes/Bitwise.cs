using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Xor
internal record XorOperatorPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_S,    // 15	002F	ldloc.s	V_6 (6)
        CilOpCodes.Xor,        // 16	0031	xor
        CilOpCodes.Newobj,     // 17	0032	newobj	instance void Class29::.ctor(int32)
        CilOpCodes.Ret         // 18	0037	ret
    };

    public bool InterchangeLdlocOpCodes => true;
}

internal record Xor : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Callvirt,    // 4	0008	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_1,     // 5	000D	stloc.1
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldarg_0,     // 7	000F	ldarg.0
        CilOpCodes.Ldloc_1,     // 8	0010	ldloc.1
        CilOpCodes.Ldloc_0,     // 9	0011	ldloc.0
        CilOpCodes.Callvirt,    // 10	0012	callvirt	instance class VMOperandType VM::method_95(class VMOperandType, class VMOperandType)
        CilOpCodes.Callvirt,    // 11	0017	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Xor;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var operatorMethod = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[10].Operand as SerializedMethodDefinition;
        return PatternMatcher
            .GetAllMatchingInstructions(new XorOperatorPattern(), operatorMethod?.CilMethodBody?.Instructions!)
            .Count > 1;
    }
}
#endregion Xor