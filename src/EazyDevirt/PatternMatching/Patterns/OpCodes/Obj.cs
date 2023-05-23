using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Devirtualization;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

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

    public CilOpCode? CilOpCode => CilOpCodes.Newobj;

    public bool Verify(VMOpCode vmOpCode, int index) => PatternMatcher.MatchesPattern(new RunNewObjectPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Newobj

#region Initobj

internal record InitobjInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Callvirt,    // 16	0022	callvirt	instance class [mscorlib]System.Reflection.FieldInfo Class43::method_5()
        CilOpCodes.Stloc_2,     // 17	0027	stloc.2
        CilOpCodes.Ldarg_2,     // 18	0028	ldarg.2
        CilOpCodes.Callvirt,    // 19	0029	callvirt	instance object VMOperandType::GetOperandValue()
        CilOpCodes.Ldloc_2,     // 20	002E	ldloc.2
        CilOpCodes.Callvirt,    // 21	002F	callvirt	instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
        CilOpCodes.Call,        // 22	0034	call	class VMOperandType VMOperandType::ConvertToVMOperand(object, class [mscorlib]System.Type)
        CilOpCodes.Stloc_S,     // 23	0039	stloc.s	V_5 (5)
        CilOpCodes.Ldloc_2,     // 24	003B	ldloc.2
        CilOpCodes.Ldloc_1,     // 25	003C	ldloc.1
        CilOpCodes.Callvirt,    // 26	003D	callvirt	instance object Class43::method_3()
        CilOpCodes.Ldloc_S,     // 27	0042	ldloc.s	V_5 (5)
        CilOpCodes.Callvirt,    // 28	0044	callvirt	instance object VMOperandType::GetOperandValue()
        CilOpCodes.Callvirt,    // 29	0049	callvirt	instance void [mscorlib]System.Reflection.FieldInfo::SetValue(object, object)
    };
    
    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
    
    public bool Verify(CilInstructionCollection instructions, int index)
    {
        if (instructions[index].Operand is not SerializedMethodDefinition firstCall ||
            firstCall.Signature == null || !firstCall.Signature!.ReturnsValue ||
            firstCall.Signature.ReturnType.FullName != "System.Reflection.FieldInfo")
            return false;

        return instructions.Any(x =>
            x.OpCode == CilOpCodes.Ldfld && x.Operand as SerializedFieldDefinition ==
            DevirtualizationContext.Instance.VMLocalsField);
    }
}

internal record Initobj : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 59	008B	ldarg.0
        CilOpCodes.Ldloc_2,     // 60	008C	ldloc.2
        CilOpCodes.Newobj,      // 61	008D	newobj	instance void VMObjectOperand::.ctor()
        CilOpCodes.Callvirt,    // 62	0092	callvirt	instance void VM::method_57(class Class38, class VMOperandType)
        CilOpCodes.Ret          // 63	0097	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Initobj;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    
    public bool Verify(VMOpCode vmOpCode, int index) => PatternMatcher.MatchesPattern(new InitobjInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 3].Operand as SerializedMethodDefinition)!);
}

#endregion Initobj