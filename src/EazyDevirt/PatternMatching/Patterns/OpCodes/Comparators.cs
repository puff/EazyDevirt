using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Clt

internal record CltInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_2,     // 71	00C2	ldloc.2
        CilOpCodes.Ldloc_3,     // 72	00C3	ldloc.3
        CilOpCodes.Blt_S,       // 73	00C4	blt.s	80 (00D6) ldc.i4.1 
        CilOpCodes.Ldloc_2,     // 74	00C6	ldloc.2
        CilOpCodes.Call,        // 75	00C7	call	bool [mscorlib]System.Double::IsNaN(float64)
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 4].Operand as SerializedMemberReference)?.FullName ==
        "System.Boolean System.Double::IsNaN(System.Double)";
}

internal record Clt : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Call,        // 9	0011	call	bool VM::CltInner(class VMOperandType, class VMOperandType)
        CilOpCodes.Brtrue_S,    // 10	0016	brtrue.s	13 (001B) ldc.i4.1 
        CilOpCodes.Ldc_I4_0,    // 11	0018	ldc.i4.0
        CilOpCodes.Br_S,        // 12	0019	br.s	14 (001C) newobj instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Ldc_I4_1,    // 13	001B	ldc.i4.1
    };

    public CilOpCode? CilOpCode => CilOpCodes.Clt;

    public bool MatchEntireBody => false;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new CltInnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index].Operand as SerializedMethodDefinition)!);
}
#endregion Clt