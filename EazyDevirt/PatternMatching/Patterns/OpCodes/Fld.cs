using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Ldfld
internal record Ldfld : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 27	003F	ldarg.0
        CilOpCodes.Ldloc_1,     // 28	0040	ldloc.1
        CilOpCodes.Ldloc_3,     // 29	0041	ldloc.3
        CilOpCodes.Callvirt,    // 30	0042	callvirt	instance object [mscorlib]System.Reflection.FieldInfo::GetValue(object)
        CilOpCodes.Ldloc_1,     // 31	0047	ldloc.1
        CilOpCodes.Callvirt,    // 32	0048	callvirt	instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
        CilOpCodes.Call,        // 33	004D	call	class VMOperandType VMOperandType::smethod_0(object, class [mscorlib]System.Type)
        CilOpCodes.Callvirt,    // 34	0052	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 35	0057	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldfld;

    public bool MatchEntireBody => false;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        if (instructions.Count < index + 3) return false;
        return (instructions[index + 3].Operand as SerializedMemberReference)?.FullName == "System.Object System.Reflection.FieldInfo::GetValue(System.Object)";
    }
}

internal record Ldflda : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 25	003C	ldarg.0
        CilOpCodes.Ldloc_S,     // 26	003D	ldloc.s	V_4 (4)
        CilOpCodes.Ldloc_2,     // 27	003F	ldloc.2
        CilOpCodes.Ldloc_0,     // 28	0040	ldloc.0
        CilOpCodes.Newobj,      // 29	0041	newobj	instance void Class43::.ctor(class [mscorlib]System.Reflection.FieldInfo, object, class Class38)
        CilOpCodes.Callvirt,    // 30	0046	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 31	004B	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldflda;

    public bool MatchEntireBody => false;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        if (instructions.Count < index + 4) return false;
        var method = instructions[index + 4].Operand as SerializedMethodDefinition;
        return method?.Parameters.Count == 3 && method.Parameters[0].ParameterType.FullName == "System.Reflection.FieldInfo";
    }
}
#endregion Ldfld

#region Ldsfld
internal record Ldsfld : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance class [mscorlib]System.Reflection.FieldInfo VM::ResolveFieldCache(int32)
        CilOpCodes.Stloc_1,     // 7	0013	stloc.1
        CilOpCodes.Ldarg_0,     // 8	0014	ldarg.0
        CilOpCodes.Ldloc_1,     // 9	0015	ldloc.1
        CilOpCodes.Ldnull,      // 10	0016	ldnull
        CilOpCodes.Callvirt,    // 11	0017	callvirt	instance object [mscorlib]System.Reflection.FieldInfo::GetValue(object)
        CilOpCodes.Ldloc_1,     // 12	001C	ldloc.1
        CilOpCodes.Callvirt,    // 13	001D	callvirt	instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
        CilOpCodes.Call,        // 14	0022	call	class VMOperandType VMOperandType::smethod_0(object, class [mscorlib]System.Type)
        CilOpCodes.Callvirt,    // 15	0027	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 16	002C	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldsfld;
    
    public bool Verify(VMOpCode vmOpCode, int index) => (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[11].Operand as SerializedMemberReference)?
        .FullName == "System.Object System.Reflection.FieldInfo::GetValue(System.Object)";
}

internal record Ldsflda : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance class [mscorlib]System.Reflection.FieldInfo VM::ResolveFieldCache(int32)
        CilOpCodes.Stloc_1,     // 7	0013	stloc.1
        CilOpCodes.Ldarg_0,     // 8	0014	ldarg.0
        CilOpCodes.Ldloc_1,     // 9	0015	ldloc.1
        CilOpCodes.Ldnull,      // 10	0016	ldnull
        CilOpCodes.Newobj,      // 11	0017	newobj	instance void Class43::.ctor(class [mscorlib]System.Reflection.FieldInfo, object)
        CilOpCodes.Callvirt,    // 12	001C	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 13	0021	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldsflda;
    
    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var method = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[11].Operand as SerializedMethodDefinition;
        return method?.Parameters.Count == 2 && method.Parameters[0].ParameterType.FullName == "System.Reflection.FieldInfo";
    }
}
#endregion Ldsfld

#region Stfld
internal record Stfld : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_2,     // 36	005E	ldloc.2
        CilOpCodes.Ldloc_0,     // 37	005F	ldloc.0
        CilOpCodes.Ldloc_S,     // 38	0060	ldloc.s	V_5 (5)
        CilOpCodes.Callvirt,    // 39	0062	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Callvirt,    // 40	0067	callvirt	instance void [mscorlib]System.Reflection.FieldInfo::SetValue(object, object)
        CilOpCodes.Ldloc_1,     // 41	006C	ldloc.1
    };

    public CilOpCode CilOpCode => CilOpCodes.Stfld;

    public bool MatchEntireBody => false;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        if (instructions.Count < index + 4) return false;
        return (instructions[index + 4].Operand as SerializedMemberReference)?.FullName == "System.Void System.Reflection.FieldInfo::SetValue(System.Object, System.Object)";
    }
}

internal record Stsfld : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_1,     // 15	002B	ldloc.1
        CilOpCodes.Ldnull,      // 16	002C	ldnull
        CilOpCodes.Ldloc_2,     // 17	002D	ldloc.2
        CilOpCodes.Callvirt,    // 18	002E	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Callvirt,    // 19	0033	callvirt	instance void [mscorlib]System.Reflection.FieldInfo::SetValue(object, object)
        CilOpCodes.Ret          // 20	0038	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Stsfld;

    public bool MatchEntireBody => false;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        if (instructions.Count < index + 4) return false;
        return (instructions[index + 4].Operand as SerializedMemberReference)?.FullName == "System.Void System.Reflection.FieldInfo::SetValue(System.Object, System.Object)";
    }
}
#endregion Stfld