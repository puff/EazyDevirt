using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

internal record Ldstr : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance string VM::ResolveString(int32)
        CilOpCodes.Stloc_1,     // 7	0013	stloc.1
        CilOpCodes.Ldarg_0,     // 8	0014	ldarg.0
        CilOpCodes.Newobj,      // 9	0015	newobj	instance void VMStringOperand::.ctor()
        CilOpCodes.Dup,         // 10	001A	dup
        CilOpCodes.Ldloc_1,     // 11	001B	ldloc.1
        CilOpCodes.Callvirt,    // 12	001C	callvirt	instance void VMStringOperand::method_4(string)
        CilOpCodes.Callvirt,    // 13	0021	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 14	0026	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldstr;

    public bool Verify(VMOpCode vmOpCode, int index) => (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[6].Operand as SerializedMethodDefinition)!.Signature!.ReturnType.FullName 
                                                        == "System.String";
}

#region Return
internal record EnableReturnFromVMMethodPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Stfld,       // 2	0002	stfld	bool VM::ReturnFromVMMethod
        CilOpCodes.Ret          // 3	0007	ret
    };
}

internal record Ret : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance void VM::EnableReturnFromVMMethod()
        CilOpCodes.Ret          // 2	0006	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ret;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new EnableReturnFromVMMethodPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand as SerializedMethodDefinition)!);
}
#endregion Return

#region Ldnull
internal record Ldnull : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Newobj,      // 1	0001	newobj	instance void VMObjectOperand::.ctor()
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	000B	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldnull;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var vmObjectOperandCtor = instructions[1].Operand as SerializedMethodDefinition;
        if (vmObjectOperandCtor?.Name != ".ctor") return false;

        var declaringType = vmObjectOperandCtor.DeclaringType!;
        if (declaringType.Fields.Count != 1 ||
            declaringType.Fields[0].Signature!.FieldType.FullName != "System.Object") return false;
        
        var baseType = declaringType.BaseType?.Resolve();
        return baseType is { Fields.Count: 2 } && baseType.Fields.Count(f => f.Signature!.FieldType.FullName == "System.Int32") == 1 || baseType!.Fields.Count(f => f.Signature!.FieldType.FullName == "System.Type") == 1;
    }
}
#endregion Ldnull

internal record Dup : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PeekStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Ldloc_0,     // 4	0008	ldloc.0
        CilOpCodes.Callvirt,    // 5	0009	callvirt	instance class VMOperandType VMOperandType::vmethod_3()
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 7	0013	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Dup;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new PeekStackPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand as SerializedMethodDefinition)!);
}

#region Newobj

internal record RunNewObjectPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_S,     // 3	000B	stloc.s	V_8 (8)
        CilOpCodes.Ldarg_0,     // 4	000D	ldarg.0
        CilOpCodes.Ldloc_S,     // 5	000E	ldloc.s	V_8 (8)
        CilOpCodes.Call,        // 6	0010	call	instance class [mscorlib]System.Reflection.MethodBase VM::ResolveMethod(int32)
        CilOpCodes.Stloc_2,     // 7	0015	stloc.2
        CilOpCodes.Ldloc_2,     // 8	0016	ldloc.2
        CilOpCodes.Callvirt,    // 9	0017	callvirt	instance class [mscorlib]System.Type [mscorlib]System.Reflection.MemberInfo::get_DeclaringType()
        CilOpCodes.Stloc_S,     // 10	001C	stloc.s	V_9 (9)
        CilOpCodes.Ldloc_2,     // 11	001E	ldloc.2
        CilOpCodes.Callvirt,    // 12	001F	callvirt	instance class [mscorlib]System.Reflection.ParameterInfo[] [mscorlib]System.Reflection.MethodBase::GetParameters()
                                // ...
    };
    
    public bool MatchEntireBody => false;

    public bool Verify(CilInstructionCollection instructions, int index)
    {
        if ((instructions[index + 6].Operand as SerializedMethodDefinition)!.Signature!.ReturnType.FullName != "System.Reflection.MethodBase")
            return false;

        return PatternMatcher.MatchesPattern(new PushStackPattern(), 
            (instructions[^2].Operand as SerializedMethodDefinition)!);
    }
}

internal record Newobj : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::RunNewObject(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Newobj;

    public bool Verify(VMOpCode vmOpCode, int index) => PatternMatcher.MatchesPattern(new RunNewObjectPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Newobj

#region Pop

internal record Pop : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class Class20 VM::PopStack()
        CilOpCodes.Pop,         // 2	0006	pop
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Pop;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions;
        return instructions![2].OpCode == CilOpCodes.Pop && PatternMatcher.GetAllMatchingInstructions(
            new PopStackPattern(),
            (instructions[1].Operand as SerializedMethodDefinition)!).Count == 1;
    }
}
#endregion Pop