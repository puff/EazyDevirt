using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Add

internal record AddOperandsPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Br_S,        // 24	0039	br.s	29 (003F) ldloc.2 
        CilOpCodes.Ldloc_0,     // 25	003B	ldloc.0
        CilOpCodes.Ldloc_1,     // 26	003C	ldloc.1
        CilOpCodes.Add,         // 27	003D	add
        CilOpCodes.Stloc_2,     // 28	003E	stloc.2
        CilOpCodes.Ldloc_2,     // 29	003F	ldloc.2
        CilOpCodes.Newobj,      // 30	0040	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Ret          // 31	0045	ret
    };

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record AddOperandsHelperPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Call,        // 4	0008	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_1,     // 5	000D	stloc.1
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldloc_1,     // 7	000F	ldloc.1
        CilOpCodes.Ldloc_0,     // 8	0010	ldloc.0
        CilOpCodes.Ldarg_1,     // 9	0011	ldarg.1
        CilOpCodes.Ldarg_2,     // 10	0012	ldarg.2
        CilOpCodes.Call,        // 11	0013	call	class VMOperandType VM::AddOperands(class VMOperandType, class VMOperandType, bool, bool)
        CilOpCodes.Call,        // 12	0018	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 13	001D	ret
    };

    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var addOperandsMethod = instructions[11].Operand as SerializedMethodDefinition;
        
        return PatternMatcher.MatchesPattern(new AddOperandsPattern(), addOperandsMethod!);
    }
}

internal record Add : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,         // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,        // 1	0001	ldc.i4.0
        CilOpCodes.Ldc_I4_0,        // 2	0002	ldc.i4.0
        CilOpCodes.Callvirt,        // 3	0003	callvirt	instance void VM::AddOperandsHelper(bool, bool)
        CilOpCodes.Ret              // 4	0008	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Add;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var addOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new AddOperandsHelperPattern(), addOperandsHelperMethod!);
    }
}

internal record Add_Ovf : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,         // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,        // 1	0001	ldc.i4.1
        CilOpCodes.Ldc_I4_0,        // 2	0002	ldc.i4.0
        CilOpCodes.Callvirt,        // 3	0003	callvirt	instance void VM::AddOperandsHelper(bool, bool)
        CilOpCodes.Ret              // 4	0008	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Add_Ovf;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var addOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new AddOperandsHelperPattern(), addOperandsHelperMethod!);
    }
}

internal record Add_Ovf_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,         // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,        // 1	0001	ldc.i4.1
        CilOpCodes.Ldc_I4_1,        // 2	0002	ldc.i4.1
        CilOpCodes.Callvirt,        // 3	0003	callvirt	instance void VM::AddOperandsHelper(bool, bool)
        CilOpCodes.Ret              // 4	0008	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Add_Ovf_Un;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var addOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new AddOperandsHelperPattern(), addOperandsHelperMethod!);
    }
}
#endregion Add

#region Sub

internal record SubOperandsPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Br_S,        // 24	0039	br.s	29 (003F) ldloc.2 
        CilOpCodes.Ldloc_0,     // 25	003B	ldloc.0
        CilOpCodes.Ldloc_1,     // 26	003C	ldloc.1
        CilOpCodes.Sub,         // 27	003D	sub
        CilOpCodes.Stloc_2,     // 28	003E	stloc.2
        CilOpCodes.Ldloc_2,     // 29	003F	ldloc.2
        CilOpCodes.Newobj,      // 30	0040	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Ret          // 31	0045	ret
    };

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record SubOperandsHelperPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Call,        // 4	0008	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_1,     // 5	000D	stloc.1
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldloc_1,     // 7	000F	ldloc.1
        CilOpCodes.Ldloc_0,     // 8	0010	ldloc.0
        CilOpCodes.Ldarg_1,     // 9	0011	ldarg.1
        CilOpCodes.Ldarg_2,     // 10	0012	ldarg.2
        CilOpCodes.Call,        // 11	0013	call	class VMOperandType VM::SubOperands(class VMOperandType, class VMOperandType, bool, bool)
        CilOpCodes.Call,        // 12	0018	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 13	001D	ret
    };
    
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var subOperandsMethod = instructions[11].Operand as SerializedMethodDefinition;
        
        return PatternMatcher.MatchesPattern(new SubOperandsPattern(), subOperandsMethod!);
    }
}

internal record Sub : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,         // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,        // 1	0001	ldc.i4.0
        CilOpCodes.Ldc_I4_0,        // 2	0002	ldc.i4.0
        CilOpCodes.Callvirt,        // 3	0003	callvirt	instance void VM::SubOperandsHelper(bool, bool)
        CilOpCodes.Ret              // 4	0008	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Sub;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var subOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new SubOperandsHelperPattern(), subOperandsHelperMethod!);
    }
}

internal record Sub_Ovf : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,         // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,        // 1	0001	ldc.i4.1
        CilOpCodes.Ldc_I4_0,        // 2	0002	ldc.i4.0
        CilOpCodes.Callvirt,        // 3	0003	callvirt	instance void VM::SubOperandsHelper(bool, bool)
        CilOpCodes.Ret              // 4	0008	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Sub_Ovf;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var subOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new SubOperandsHelperPattern(), subOperandsHelperMethod!);
    }
}

internal record Sub_Ovf_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,         // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,        // 1	0001	ldc.i4.1
        CilOpCodes.Ldc_I4_1,        // 2	0002	ldc.i4.1
        CilOpCodes.Callvirt,        // 3	0003	callvirt	instance void VM::SubOperandsHelper(bool, bool)
        CilOpCodes.Ret              // 4	0008	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Sub_Ovf_Un;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var subOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new SubOperandsHelperPattern(), subOperandsHelperMethod!);
    }
}
#endregion Sub

#region Rem

internal record RemOperandsPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_3,     // 17	0031	ldloc.3
        CilOpCodes.Rem,         // 18	0032	rem
        CilOpCodes.Newobj,      // 19	0033	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Ret,         // 20	0038	ret
    };

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record RemOperandsHelperPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Call,        // 4	0008	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_1,     // 5	000D	stloc.1
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldloc_1,     // 7	000F	ldloc.1
        CilOpCodes.Ldloc_0,     // 8	0010	ldloc.0
        CilOpCodes.Ldarg_1,     // 9	0011	ldarg.1
        CilOpCodes.Call,        // 10	0012	call	class VMOperandType VM::RemOperands(class VMOperandType, class VMOperandType, bool)
        CilOpCodes.Call,        // 11	0017	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var remOperandsMethod = instructions[10].Operand as SerializedMethodDefinition;
        
        return PatternMatcher.MatchesPattern(new RemOperandsPattern(), remOperandsMethod!);
    }
}

internal record Rem : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1	0001	ldc.i4.0
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::RemOperandsHelper(bool)
        CilOpCodes.Ret          // 3	0007	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Rem;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var remOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new RemOperandsHelperPattern(), remOperandsHelperMethod!);
    }
}

internal record Rem_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::RemOperandsHelper(bool)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Rem_Un;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var remOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new RemOperandsHelperPattern(), remOperandsHelperMethod!);
    }
}
#endregion Rem

#region Div

internal record DivOperandsPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_3,     // 17	0031	ldloc.3
        CilOpCodes.Div,         // 18	0032	div
        CilOpCodes.Newobj,      // 19	0033	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Ret,         // 20	0038	ret
    };

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record DivOperandsHelperPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Call,        // 4	0008	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_1,     // 5	000D	stloc.1
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldarg_0,     // 7	000F	ldarg.0
        CilOpCodes.Ldloc_1,     // 8	0010	ldloc.1
        CilOpCodes.Ldloc_0,     // 9	0011	ldloc.0
        CilOpCodes.Ldarg_1,     // 10	0012	ldarg.1
        CilOpCodes.Call,        // 11	0013	call	instance class VMOperandType VM::DivOperands(class VMOperandType, class VMOperandType, bool)
        CilOpCodes.Call,        // 12	0018	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 13	001D	ret
    };

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var divOperandsMethod = instructions[11].Operand as SerializedMethodDefinition;
        
        return PatternMatcher.MatchesPattern(new DivOperandsPattern(), divOperandsMethod!);
    }
}

internal record Div : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1	0001	ldc.i4.0
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::DivOperandsHelper(bool)
        CilOpCodes.Ret          // 3	0007	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Div;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var divOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new DivOperandsHelperPattern(), divOperandsHelperMethod!);
    }
}

internal record Div_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::DivOperandsHelper(bool)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Div_Un;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var divOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new DivOperandsHelperPattern(), divOperandsHelperMethod!);
    }
}
#endregion Div

#region Mul

internal record MulOperandsPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,     // 20	0035	ldloc.0
        CilOpCodes.Ldloc_1,     // 21	0036	ldloc.1
        CilOpCodes.Mul_Ovf,     // 22	0037	mul.ovf
        CilOpCodes.Stloc_2,     // 23	0038	stloc.2
    };

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record MulOperandsHelperPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Call,        // 4	0008	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_1,     // 5	000D	stloc.1
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldloc_1,     // 7	000F	ldloc.1
        CilOpCodes.Ldloc_0,     // 8	0010	ldloc.0
        CilOpCodes.Ldarg_1,     // 9	0011	ldarg.1
        CilOpCodes.Ldarg_2,     // 10	0012	ldarg.2
        CilOpCodes.Call,        // 11	0013	call	class VMOperandType VM::MulOperands(class VMOperandType, class VMOperandType, bool, bool)
        CilOpCodes.Call,        // 12	0018	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 13	001D	ret
    };

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var mulOperandsMethod = instructions[11].Operand as SerializedMethodDefinition;
        
        return PatternMatcher.MatchesPattern(new MulOperandsPattern(), mulOperandsMethod!);
    }
}

internal record Mul : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1	0001	ldc.i4.0
        CilOpCodes.Ldc_I4_0,    // 2	0002	ldc.i4.0
        CilOpCodes.Callvirt,    // 3	0003	callvirt	instance void VM::MulOperandsHelper(bool, bool)
        CilOpCodes.Ret          // 4	0008	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Mul;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var mulOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new MulOperandsHelperPattern(), mulOperandsHelperMethod!);
    }
}

internal record Mul_Ovf : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Ldc_I4_0,    // 2	0002	ldc.i4.0
        CilOpCodes.Callvirt,    // 3	0003	callvirt	instance void VM::MulOperandsHelper(bool, bool)
        CilOpCodes.Ret          // 4	0008	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Mul_Ovf;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var mulOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new MulOperandsHelperPattern(), mulOperandsHelperMethod!);
    }
}

internal record Mul_Ovf_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Ldc_I4_1,    // 2	0002	ldc.i4.1
        CilOpCodes.Callvirt,    // 3	0003	callvirt	instance void VM::MulOperandsHelper(bool, bool)
        CilOpCodes.Ret          // 4	0008	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Mul_Ovf_Un;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var mulOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new MulOperandsHelperPattern(), mulOperandsHelperMethod!);
    }
}
#endregion Mul