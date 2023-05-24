using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
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

internal record Conv_Ovf_I_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloca_S,        // 16	001C	ldloca.s	V_2 (2)
        CilOpCodes.Ldloc_0,         // 17	001E	ldloc.0
        CilOpCodes.Castclass,       // 18	001F	castclass	VMLongOperand
        CilOpCodes.Callvirt,        // 19	0024	callvirt	instance int64 VMLongOperand::method_3()
        CilOpCodes.Conv_Ovf_U8,     // 20	0029	conv.ovf.u8
        CilOpCodes.Conv_Ovf_I8_Un,  // 21	002A	conv.ovf.i8.un
        CilOpCodes.Call,            // 22	002B	call	instance void [mscorlib]System.IntPtr::.ctor(int64)
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I_Un;
    
    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 6].Operand as SerializedMemberReference)?.FullName ==
        "System.Void System.IntPtr::.ctor(System.Int64)";
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

internal record Conv_Ovf_I1_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,         // 20	003C	ldloc.0
        CilOpCodes.Castclass,       // 21	003D	castclass	VMIntPtrOperand
        CilOpCodes.Callvirt,        // 22	0042	callvirt	instance native int VMIntPtrOperand::method_3()
        CilOpCodes.Call,            // 23	0047	call	int32 [mscorlib]System.IntPtr::op_Explicit(native int)
        CilOpCodes.Conv_Ovf_U4,     // 24	004C	conv.ovf.u4
        CilOpCodes.Conv_Ovf_I1_Un,  // 25	004D	conv.ovf.i1.un
        CilOpCodes.Stloc_2,         // 26	004E	stloc.2
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I1_Un;
    
    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.Int32 System.IntPtr::op_Explicit(System.IntPtr)";
}

#endregion Conv_I1

#region Conv_I2

internal record Conv_I2InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 23	0047	ldloc.0
        CilOpCodes.Castclass,   // 24	0048	castclass	VMIntPtrOperand
        CilOpCodes.Callvirt,    // 25	004D	callvirt	instance native int VMIntPtrOperand::method_3()
        CilOpCodes.Call,        // 26	0052	call	int32 [mscorlib]System.IntPtr::op_Explicit(native int)
        CilOpCodes.Conv_Ovf_I2, // 27	0057	conv.ovf.i2
        CilOpCodes.Stloc_2,     // 28	0058	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.Int64 System.IntPtr::op_Explicit(System.IntPtr)";
}

internal record Conv_I2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I2Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_I2;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I2InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I2Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I2;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I2InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I2_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,         // 20	003C	ldloc.0
        CilOpCodes.Castclass,       // 21	003D	castclass	VMIntPtrOperand
        CilOpCodes.Callvirt,        // 22	0042	callvirt	instance native int VMIntPtrOperand::method_3()
        CilOpCodes.Call,            // 23	0047	call	int32 [mscorlib]System.IntPtr::op_Explicit(native int)
        CilOpCodes.Conv_Ovf_U4,     // 24	004C	conv.ovf.u4
        CilOpCodes.Conv_Ovf_I2_Un,  // 25	004D	conv.ovf.i1.un
        CilOpCodes.Stloc_2,         // 26	004E	stloc.2
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I2_Un;
    
    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.Int32 System.IntPtr::op_Explicit(System.IntPtr)";
}

#endregion Conv_I2

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
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I4Inner(bool)
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
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_I4Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I4;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I4InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I4_Un : Conv_I4InnerPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I4_Un;
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

internal record Conv_Ovf_I8_Un : Conv_I8InnerPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_I8_Un;
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
        CilOpCodes.Ldloc_0,         // 87	00F6	ldloc.0
        CilOpCodes.Castclass,       // 88	00F7	castclass	VMLongOperand
        CilOpCodes.Callvirt,        // 89	00FC	callvirt	instance int64 VMLongOperand::method_3()
        CilOpCodes.Conv_Ovf_U8,     // 90	0101	conv.ovf.u8
        CilOpCodes.Conv_Ovf_U1_Un,  // 91	0102	conv.ovf.u1.un
        CilOpCodes.Stloc_2,         // 92	0103	stloc.2
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

internal record Conv_Ovf_U1_Un : Conv_U1InnerPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U1_Un;
}

#endregion Conv_U1

#region Conv_U2

internal record Conv_U2InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,         // 87	00F6	ldloc.0
        CilOpCodes.Castclass,       // 88	00F7	castclass	VMLongOperand
        CilOpCodes.Callvirt,        // 89	00FC	callvirt	instance int64 VMLongOperand::method_3()
        CilOpCodes.Conv_Ovf_U8,     // 90	0101	conv.ovf.u8
        CilOpCodes.Conv_Ovf_U2_Un,  // 91	0102	conv.ovf.u2.un
        CilOpCodes.Stloc_2,         // 92	0103	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record Conv_U2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U2Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_U2;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U2InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_U2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U2Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U2;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U2InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_U2_Un : Conv_U2InnerPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U2_Un;
}

#endregion Conv_U2

#region Conv_U4

internal record Conv_U4InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 85	00F4	ldloc.0
        CilOpCodes.Castclass,   // 86	00F5	castclass	VMLongOperand
        CilOpCodes.Callvirt,    // 87	00FA	callvirt	instance int64 VMLongOperand::method_3()
        CilOpCodes.Conv_Ovf_U4, // 88	00FF	conv.ovf.u4
        CilOpCodes.Stloc_2,     // 89	0100	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record Conv_U4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U4Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_U4;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U4InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_U4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::Conv_U4Inner(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U4;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_U4InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_U4_Un : Conv_U4InnerPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U4_Un;
}

#endregion Conv_U4

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

internal record Conv_Ovf_U8_Un : Conv_U8InnerPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Conv_Ovf_U8_Un;
}

#endregion Conv_U8

#endregion Conv_U