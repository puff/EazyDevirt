using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;

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

    bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new SetBranchIndexPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand as SerializedMethodDefinition)!);
}

// No br.s opcode

internal record Brtrue : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 52	0097	ldloc.0
        CilOpCodes.Callvirt,    // 53	0098	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Ldnull,      // 54	009D	ldnull
        CilOpCodes.Cgt_Un,      // 55	009E	cgt.un
        CilOpCodes.Stloc_1,     // 56	00A0	stloc.1
                                // ...
    };

    public CilOpCode CilOpCode => CilOpCodes.Brtrue;

    public bool MatchEntireBody => false;
}

internal record Brfalse : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 54	009A	ldloc.0
        CilOpCodes.Callvirt,    // 55	009B	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Ldnull,      // 56	00A0	ldnull
        CilOpCodes.Ceq,         // 57	00A1	ceq
        CilOpCodes.Stloc_1,     // 58	00A3	stloc.1
                                // ...
    };

    public CilOpCode CilOpCode => CilOpCodes.Brfalse;

    public bool MatchEntireBody => false;
}

/// <summary>
/// Used by various branch opcodes
/// </summary>
internal record BrtruePattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        // 0	0000	ldarg.0
        // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        // 2	0006	stloc.0
        // 3	0007	ldarg.0
        // 4	0008	callvirt	instance class VMOperandType VM::PopStack()
        // 5	000D	ldloc.0
        // 6	000E	call	bool VM::smethod_15(class VMOperandType, class VMOperandType)
        // 7	0013	brfalse.s	15 (0028) ret
    };
    
    public bool MatchEntireBody => false;
}

/// <summary>
/// Used by various branch opcodes
/// </summary>
internal record BrfalsePattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        // 0	0000	ldarg.0
        // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        // 2	0006	stloc.0
        // 3	0007	ldarg.0
        // 4	0008	callvirt	instance class VMOperandType VM::PopStack()
        // 5	000D	ldloc.0
        // 6	000E	call	bool VM::smethod_215(class VMOperandType, class VMOperandType)
        // 7	0013	brtrue.s	15 (0028) ret 
    };

    public bool MatchEntireBody => false;
}