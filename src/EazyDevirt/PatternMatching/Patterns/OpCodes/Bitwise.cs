using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;
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

    public CilOpCode? CilOpCode => CilOpCodes.Xor;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var operatorMethod = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[10].Operand as SerializedMethodDefinition;
        return PatternMatcher
            .GetAllMatchingInstructions(new XorOperatorPattern(), operatorMethod?.CilMethodBody?.Instructions!)
            .Count > 1;
    }
}
#endregion Xor

#region Shr

internal record ShrOperandsPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_1,     // 17	002E	ldloc.1
        CilOpCodes.Ldc_I4_S,    // 18	002F	ldc.i4.s	0x1F
        CilOpCodes.And,         // 19	0031	and
        CilOpCodes.Shr,         // 20	0032	shr
        CilOpCodes.Newobj,      // 21	0033	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Ret,         // 22	0038	ret
    };

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
}

internal record ShrOperandsHelperPattern : IPattern
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
        CilOpCodes.Call,        // 10	0012	call	class VMOperandType VM::ShrOperands(class VMOperandType, class VMOperandType, bool)
        CilOpCodes.Call,        // 11	0017	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var shrOperandsMethod = instructions[10].Operand as SerializedMethodDefinition;
        
        return PatternMatcher.MatchesPattern(new ShrOperandsPattern(), shrOperandsMethod!);
    }
}

internal record Shr : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1	0001	ldc.i4.0
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::ShrOperandsHelper(bool)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Shr;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var shrOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new ShrOperandsHelperPattern(), shrOperandsHelperMethod!);
    }
}

internal record Shr_Un : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::ShrOperandsHelper(bool)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Shr_Un;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var shrOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new ShrOperandsHelperPattern(), shrOperandsHelperMethod!);
    }
}
#endregion Shr

#region And

internal record AndOperandsPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 8	0017	ldarg.1
        CilOpCodes.Castclass,   // 9	0018	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 10	001D	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_S,     // 11	0022	stloc.s	V_6 (6)
        CilOpCodes.Ldarg_2,     // 12	0024	ldarg.2
        CilOpCodes.Castclass,   // 13	0025	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 14	002A	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_S,     // 15	002F	stloc.s	V_7 (7)
        CilOpCodes.Newobj,      // 16	0031	newobj	instance void VMIntOperand::.ctor()
        CilOpCodes.Dup,         // 17	0036	dup
        CilOpCodes.Ldloc_S,     // 18	0037	ldloc.s	V_6 (6)
        CilOpCodes.Ldloc_S,     // 19	0039	ldloc.s	V_7 (7)
        CilOpCodes.And,         // 20	003B	and
        CilOpCodes.Callvirt,    // 21	003C	callvirt	instance void VMIntOperand::method_4(int32)
        CilOpCodes.Ret,         // 22	0041	ret
    };

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record And : IOpCodePattern
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
        CilOpCodes.Callvirt,    // 10	0012	callvirt	instance class VMOperandType VM::AndOperands(class VMOperandType, class VMOperandType)
        CilOpCodes.Callvirt,    // 11	0017	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.And;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var andOperandsHelperMethod =
            vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[10].Operand as SerializedMethodDefinition;

        return PatternMatcher.MatchesPattern(new AndOperandsPattern(), andOperandsHelperMethod!);
    }
}
#endregion And