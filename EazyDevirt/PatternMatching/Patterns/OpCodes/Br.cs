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

    public CilOpCode? CilOpCode => CilOpCodes.Br;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new SetBranchIndexPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand as SerializedMethodDefinition)!);
}

#region Bgt

internal record BgtInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 97	0102	ldarg.0
        CilOpCodes.Castclass,   // 98	0103	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 99	0108	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Ldarg_1,     // 100	010D	ldarg.1
        CilOpCodes.Castclass,   // 101	010E	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 102	0113	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Cgt,         // 103	0118	cgt
        CilOpCodes.Stloc_0,     // 104	011A	stloc.0
    };

    public bool MatchEntireBody => false;
    
    public bool InterchangeStlocOpCodes => true;
}

internal record Bgt : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Callvirt,    // 4	0008	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Call,        // 6	000E	call	bool VM::BgtInner(class VMOperandType, class VMOperandType)
        CilOpCodes.Brfalse_S,   // 7	0013	brfalse.s	15 (0028) ret 
    };

    public CilOpCode? CilOpCode => CilOpCodes.Bgt;

    public bool MatchEntireBody => false;
    
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) => PatternMatcher.MatchesPattern(new BgtInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand as SerializedMethodDefinition)!);
}
#endregion Bgt

#region Blt

internal record BltInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 63	00AA	ldarg.0
        CilOpCodes.Castclass,   // 64	00AB	castclass	VMDoubleOperand
        CilOpCodes.Callvirt,    // 65	00B0	callvirt	instance float64 VMDoubleOperand::method_3()
        CilOpCodes.Ldarg_1,     // 66	00B5	ldarg.1
        CilOpCodes.Castclass,   // 67	00B6	castclass	VMDoubleOperand
        CilOpCodes.Callvirt,    // 68	00BB	callvirt	instance float64 VMDoubleOperand::method_3()
        CilOpCodes.Clt,         // 69	00C0	clt
        CilOpCodes.Stloc_0,     // 70	00C2	stloc.0
    };

    public bool MatchEntireBody => false;
    
    public bool InterchangeStlocOpCodes => true;
}

internal record Blt : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Callvirt,    // 4	0008	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Call,        // 6	000E	call	bool VM::BltInner(class VMOperandType, class VMOperandType)
        CilOpCodes.Brfalse_S,   // 7	0013	brfalse.s	15 (0028) ret 
    };

    public CilOpCode? CilOpCode => CilOpCodes.Blt;

    public bool MatchEntireBody => false;
    
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) => PatternMatcher.MatchesPattern(new BltInnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand as SerializedMethodDefinition)!);
}
#endregion Blt

#region Beq

internal record BeqInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 283	03CC	ldarg.0
        CilOpCodes.Callvirt,    // 284	03CD	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Ldarg_1,     // 285	03D2	ldarg.1
        CilOpCodes.Callvirt,    // 286	03D3	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Ceq,         // 287	03D8	ceq
        CilOpCodes.Stloc_0,     // 288	03DA	stloc.0
    };

    public bool MatchEntireBody => false;
    
    public bool InterchangeStlocOpCodes => true;
}

internal record Beq : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Callvirt,    // 4	0008	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Call,        // 6	000E	call	bool VM::BeqInner(class VMOperandType, class VMOperandType)
        CilOpCodes.Brfalse_S,   // 7	0013	brfalse.s	15 (0028) ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Beq;

    public bool MatchEntireBody => false;
    
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) => PatternMatcher.MatchesPattern(new BeqInnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand as SerializedMethodDefinition)!);
}
#endregion Beq

internal record Brtrue : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 52	0097	ldloc.0
        CilOpCodes.Callvirt,    // 53	0098	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Ldnull,      // 54	009D	ldnull
        CilOpCodes.Cgt_Un,      // 55	009E	cgt.un
        CilOpCodes.Stloc_1,     // 56	00A0	stloc.1
    };

    public CilOpCode? CilOpCode => CilOpCodes.Brtrue;

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
    };

    public CilOpCode? CilOpCode => CilOpCodes.Brfalse;

    public bool MatchEntireBody => false;
}