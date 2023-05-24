using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;
// ReSharper disable InconsistentNaming

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Conv_IC

#region Conv_I

internal record Conv_IInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloca_S,    // 93	00ED	ldloca.s	V_6 (6)
        CilOpCodes.Ldloc_3,     // 94	00EF	ldloc.3
        CilOpCodes.Conv_Ovf_I8, // 95	00F0	conv.ovf.i8
        CilOpCodes.Call,        // 96	00F1	call	instance void [mscorlib]System.IntPtr::.ctor(int64)
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.Void System.IntPtr::.ctor(System.Int64)";
}


internal record Conv_I : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_IInner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_I;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_IInnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_IInner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_IInnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Conv_I

#region Conv_I1
internal record Conv_I1InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 39	0078	ldloc.0
        CilOpCodes.Castclass,   // 40	0079	castclass	VMIntPtrOperand
        CilOpCodes.Callvirt,    // 41	007E	callvirt	instance native int VMIntPtrOperand::method_3()
        CilOpCodes.Call,        // 42	0083	call	int64 [mscorlib]System.IntPtr::op_Explicit(native int)
        CilOpCodes.Conv_Ovf_I1, // 43	0088	conv.ovf.i1
        CilOpCodes.Stloc_2,     // 44	0089	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.Int64 System.IntPtr::op_Explicit(System.IntPtr)";
}


internal record Conv_I1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I1Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_I1;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I1InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I1Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I1;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I1InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Conv_I1

#region Conv_I4

internal record Conv_I4InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,         // 75	00D0	ldloc.0
        CilOpCodes.Castclass,       // 76	00D1	castclass	VMEnumOperand
        CilOpCodes.Callvirt,        // 77	00D6	callvirt	instance class [System.Runtime]System.Enum VMEnumOperand::method_3()
        CilOpCodes.Call,            // 78	00DB	call	uint64 [mscorlib]System.Convert::ToUInt64(object)
        CilOpCodes.Conv_Ovf_I4_Un,  // 79	00E0	conv.ovf.i4.un
        CilOpCodes.Stloc_2,         // 80	00E1	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.UInt64 System.Convert::ToUInt64(System.Object)";
}


internal record Conv_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I4Operand(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_I4;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I4InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I4Operand(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I4;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I4InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Conv_I4

#region Conv_I8

internal record Conv_I8InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,         // 75	00D0	ldloc.0
        CilOpCodes.Castclass,       // 76	00D1	castclass	VMEnumOperand
        CilOpCodes.Callvirt,        // 77	00D6	callvirt	instance class [System.Runtime]System.Enum VMEnumOperand::method_3()
        CilOpCodes.Call,            // 78	00DB	call	uint64 [mscorlib]System.Convert::ToUInt64(object)
        CilOpCodes.Conv_Ovf_I8_Un,  // 79	00E0	conv.ovf.i8.un
        CilOpCodes.Stloc_2,         // 80	00E1	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.UInt64 System.Convert::ToUInt64(System.Object)";
}


internal record Conv_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I8Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_I8;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I8InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I8Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I8;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I8InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Conv_I8

#endregion Conv_IC

#region Conv_RC

#region Conv_R_Un

internal record Conv_R_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 31	0041	ldloc.0
        CilOpCodes.Castclass,   // 32	0042	castclass	VMEnumOperand
        CilOpCodes.Callvirt,    // 33	0047	callvirt	instance class [System.Runtime]System.Enum VMEnumOperand::method_3()
        CilOpCodes.Call,        // 34	004C	call	uint64 [mscorlib]System.Convert::ToUInt64(object)
        CilOpCodes.Conv_R_Un,   // 35	0051	conv.r.un
        CilOpCodes.Conv_R8,     // 36	0052	conv.r8
        CilOpCodes.Stloc_2,     // 37	0053	stloc.2
        // this is from the original executable because de4dot optimizes this
        CilOpCodes.Br_S,        // 36	004E	br.s	39 (0056) ldarg.0 
        CilOpCodes.Newobj,      // 37	0050	newobj	instance void [mscorlib]System.InvalidOperationException::.ctor()
        CilOpCodes.Throw,       // 38	0055	throw
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_R_Un;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
 
    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        instructions[3 + index].Operand is SerializedMemberReference
        {
            FullName: "System.UInt64 System.Convert::ToUInt64(System.Object)"
        };
}
#endregion Conv_R_Un

#region Conv_R8

internal record Conv_R8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 22	002B	ldloc.0
        CilOpCodes.Castclass,   // 23	002C	castclass	VMEnumOperand
        CilOpCodes.Callvirt,    // 24	0031	callvirt	instance class [System.Runtime]System.Enum VMEnumOperand::method_3()
        CilOpCodes.Call,        // 25	0036	call	uint64 [mscorlib]System.Convert::ToUInt64(object)
        CilOpCodes.Conv_R_Un,   // 26	003B	conv.r.un
        CilOpCodes.Conv_R8,     // 27	003C	conv.r8
        CilOpCodes.Stloc_2,     // 28	003D	stloc.2
        CilOpCodes.Br_S,        // 29	003E	br.s	48 (006B) ldarg.0
        CilOpCodes.Ldloc_1,     // 30	0040	ldloc.1
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_R8;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        instructions[3 + index].Operand is SerializedMemberReference
        {
            FullName: "System.UInt64 System.Convert::ToUInt64(System.Object)"
        };
}
#endregion Conv_R8

#endregion Conv_RC

#region Conv_U

#region Conv_U1

internal record Conv_U1InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 94	0106	ldloc.0
        CilOpCodes.Castclass,   // 95	0107	castclass	VMLongOperand
        CilOpCodes.Callvirt,    // 96	010C	callvirt	instance int64 VMLongOperand::method_3()
        CilOpCodes.Conv_U1,     // 97	0111	conv.u1
        CilOpCodes.Stloc_2,     // 98	0112	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}


internal record Conv_U1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U1Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_U1;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U1InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_U1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U1Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U1;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U1InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Conv_U1

#region Conv_U8

internal record Conv_U8InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 86	00F2	ldloc.0
        CilOpCodes.Castclass,   // 87	00F3	castclass	VMLongOperand
        CilOpCodes.Callvirt,    // 88	00F8	callvirt	instance int64 VMLongOperand::method_3()
        CilOpCodes.Conv_Ovf_U8, // 89	00FD	conv.ovf.u8
        CilOpCodes.Stloc_2,     // 90	00FE	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}


internal record Conv_U8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U8Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_U8;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U8InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_U8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U8Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U8;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U8InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Conv_U8
#endregion Conv_U