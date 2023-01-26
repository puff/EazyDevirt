using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

internal record SetBranchIndexPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Newobj,      // 2	0002	newobj	instance void valuetype [mscorlib]System.Nullable`1<uint32>::.ctor(!0)
        CilOpCodes.Stfld,       // 3	0007	stfld	valuetype [mscorlib]System.Nullable`1<uint32> VM::branchIndex
        CilOpCodes.Ret          // 4	000C	ret
    };
}

internal record Br : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMUIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance uint32 VMUIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance void VM::SetBranchIndex(uint32)
        CilOpCodes.Ret          // 7	0013	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Br;

    bool IOpCodePattern.Verify(VMOpCode vmOpCode, int index)
    {
        return PatternMatcher.MatchesPattern(new SetBranchIndexPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand as SerializedMethodDefinition)!);
    }
}