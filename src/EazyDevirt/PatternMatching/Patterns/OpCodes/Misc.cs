using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;
// ReSharper disable InconsistentNaming

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Ldstr

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

    public CilOpCode? CilOpCode => CilOpCodes.Ldstr;

    public bool Verify(VMOpCode vmOpCode, int index) => (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[6].Operand as SerializedMethodDefinition)!.Signature!.ReturnType.FullName 
                                                        == "System.String";
}
#endregion Ldstr

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

    public CilOpCode? CilOpCode => CilOpCodes.Ret;

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

    public CilOpCode? CilOpCode => CilOpCodes.Ldnull;

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

#region Dup

internal record Dup : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PeekStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Ldloc_0,     // 4	0008	ldloc.0
        CilOpCodes.Callvirt,    // 5	0009	callvirt	instance class VMOperandType VMOperandType::Clone()
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 7	0013	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Dup;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new PeekStackPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand as SerializedMethodDefinition)!);
}
#endregion Dup

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

    public CilOpCode? CilOpCode => CilOpCodes.Pop;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions;
        return instructions![2].OpCode == CilOpCodes.Pop && PatternMatcher.GetAllMatchingInstructions(
            new PopStackPattern(),
            (instructions[1].Operand as SerializedMethodDefinition)!).Count == 1;
    }
}
#endregion Pop

#region Ldtoken

internal record Ldtoken : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 33	0064	ldarg.0
        CilOpCodes.Ldloc_0,     // 34	0065	ldloc.0
        CilOpCodes.Callvirt,    // 35	0066	callvirt	instance class [mscorlib]System.Reflection.FieldInfo VM::ResolveField(int32)
        CilOpCodes.Callvirt,    // 36	006B	callvirt	instance valuetype [mscorlib]System.RuntimeFieldHandle [mscorlib]System.Reflection.FieldInfo::get_FieldHandle()
        CilOpCodes.Box,         // 37	0070	box	[mscorlib]System.RuntimeFieldHandle
        CilOpCodes.Stloc_2,     // 38	0075	stloc.2
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldtoken;

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions!;
        var resolveFieldCall = instructions[index + 2].Operand as SerializedMethodDefinition;
        if (!resolveFieldCall!.Signature!.ReturnsValue ||
            resolveFieldCall.Signature.ReturnType.FullName != "System.Reflection.FieldInfo")
            return false;
        
        var getFieldHandleCall = instructions[index + 3].Operand as SerializedMemberReference;
        return getFieldHandleCall!.FullName == "System.RuntimeFieldHandle System.Reflection.FieldInfo::get_FieldHandle()";
    }
}
#endregion Ldtoken

#region Box

internal record Box : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Ldc_I4_1,    // 6	000E	ldc.i4.1
        CilOpCodes.Callvirt,    // 7	000F	callvirt	instance class [mscorlib]System.Type VM::ResolveType(int32, bool)
        CilOpCodes.Stloc_1,     // 8	0014	stloc.1
        CilOpCodes.Ldarg_0,     // 9	0015	ldarg.0
        CilOpCodes.Callvirt,    // 10	0016	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Callvirt,    // 11	001B	callvirt	instance object VMOperandType::GetOperandValue()
        CilOpCodes.Ldloc_1,     // 12	0020	ldloc.1
        CilOpCodes.Call,        // 13	0021	call	class VMOperandType VMOperandType::ConvertToVMOperand(object, class [mscorlib]System.Type)
        CilOpCodes.Stloc_2,     // 14	0026	stloc.2
        CilOpCodes.Ldloc_2,     // 15	0027	ldloc.2
        CilOpCodes.Ldloc_1,     // 16	0028	ldloc.1
        CilOpCodes.Callvirt,    // 17	0029	callvirt	instance void VMOperandType::SetOperandType(class [mscorlib]System.Type)
        CilOpCodes.Ldarg_0,     // 18	002E	ldarg.0
        CilOpCodes.Ldloc_2,     // 19	002F	ldloc.2
        CilOpCodes.Callvirt,    // 20	0030	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 21	0035	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Box;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions!;
        var resolveTypeCall = instructions[index + 7].Operand as SerializedMethodDefinition;
        if (!resolveTypeCall!.Signature!.ReturnsValue ||
            resolveTypeCall.Signature.ReturnType.FullName != "System.Type")
            return false;

        var boxOperandCall = instructions[index + 13].Operand as SerializedMethodDefinition;
        return boxOperandCall?.Parameters.Count == 2 &&
               boxOperandCall.Parameters[0].ParameterType.FullName == "System.Object" &&
               boxOperandCall.Parameters[1].ParameterType.FullName == "System.Type";
    }
}
#endregion Box

#region Unbox

internal record Unbox : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ret          // 0	0000	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Unbox;
    
    public bool Verify(VMOpCode vmOpCode, int index = 0) => vmOpCode.CilOperandType == CilOperandType.InlineTok;
}

#endregion Unbox

#region Unbox_Any

internal record Unbox_Any : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 9	0015	ldarg.0
        CilOpCodes.Callvirt,    // 10	0016	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Callvirt,    // 11	001B	callvirt	instance object VMOperandType::GetOperandValue()
        CilOpCodes.Ldloc_1,     // 12	0020	ldloc.1
        CilOpCodes.Call,        // 13	0021	call	class VMOperandType VMOperandType::ConvertToVMOperand(object, class [mscorlib]System.Type)
        CilOpCodes.Stloc_2,     // 14	0026	stloc.2
        CilOpCodes.Ldarg_0,     // 15	0027	ldarg.0
        CilOpCodes.Ldloc_2,     // 16	0028	ldloc.2
        CilOpCodes.Callvirt,    // 17	0029	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 18	002E	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Unbox_Any;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        instructions[index + 4].Operand is SerializedMethodDefinition { Parameters.Count: 2 } unboxCall &&
        unboxCall.Parameters.All(x => x.ParameterType.FullName is "System.Object" or "System.Type");
}
#endregion Unbox_Any

#region Ckfinite

internal record Ckfinite : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Castclass,   // 2	0006	castclass	VMDoubleOperand
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldloc_0,     // 4	000C	ldloc.0
        CilOpCodes.Callvirt,    // 5	000D	callvirt	instance float64 VMDoubleOperand::method_3()
        CilOpCodes.Call,        // 6	0012	call	bool [mscorlib]System.Double::IsNaN(float64)
        CilOpCodes.Brtrue_S,    // 7	0017	brtrue.s	12 (0026) nop 
        CilOpCodes.Ldloc_0,     // 8	0019	ldloc.0
        CilOpCodes.Callvirt,    // 9	001A	callvirt	instance float64 VMDoubleOperand::method_3()
        CilOpCodes.Call,        // 10	001F	call	bool [mscorlib]System.Double::IsInfinity(float64)
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ckfinite;

    public bool MatchEntireBody => false;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions!;
        if (instructions[index + 6].Operand is not SerializedMemberReference
            {
                FullName: "System.Boolean System.Double::IsNaN(System.Double)"
            })
            return false;

        return instructions[index + 10].Operand is SerializedMemberReference
        {
            FullName: "System.Boolean System.Double::IsInfinity(System.Double)"
        };
    }
}
#endregion Ckfinite

#region Castclass

internal record Castclass : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Newobj,      // 17	0026	newobj	instance void [mscorlib]System.InvalidCastException::.ctor()
        CilOpCodes.Throw,       // 18	002B	throw
    };

    public CilOpCode? CilOpCode => CilOpCodes.Castclass;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index = 0) =>
        vmOpCode.CilOperandType == CilOperandType.InlineTok &&
        vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[index].Operand is SerializedMemberReference
        {
            FullName: "System.Void System.InvalidCastException::.ctor()"
        };
}
#endregion Castclass

#region Isinst

internal record IsVMOperandAssignableFromTypePattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_1,         // 15	001D	ldloc.1
        CilOpCodes.Ldarg_2,         // 16	001E	ldarg.2
        CilOpCodes.Beq_S,           // 17	001F	beq.s	61 (007F) ldc.i4.1 
        CilOpCodes.Ldarg_2,         // 18	0021	ldarg.2
        CilOpCodes.Ldloc_1,         // 19	0022	ldloc.1
        CilOpCodes.Callvirt,        // 20	0023	callvirt	instance bool [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 5].Operand as IMethodDescriptor)?.FullName ==
        "System.Boolean System.Type::IsAssignableFrom(System.Type)";
}

internal record Isinst : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Callvirt,        // 15	001F	callvirt	instance bool VM::IsVMOperandAssignableFromType(class VMOperandType, class [mscorlib]System.Type)
        CilOpCodes.Brfalse_S,       // 16	0024	brfalse.s	21 (002E) ldarg.0 
        CilOpCodes.Ldarg_0,         // 17	0026	ldarg.0
        CilOpCodes.Ldloc_2,         // 18	0027	ldloc.2
        CilOpCodes.Callvirt,        // 19	0028	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret,             // 20	002D	ret
        CilOpCodes.Ldarg_0,         // 21	002E	ldarg.0
        CilOpCodes.Newobj,          // 22	002F	newobj	instance void VMObjectOperand::.ctor()
        CilOpCodes.Callvirt,        // 23	0034	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret              // 24	0039	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Isinst;

    public bool MatchEntireBody => false;
    
    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        PatternMatcher.MatchesPattern(new IsVMOperandAssignableFromTypePattern(),
            instructions[index].Operand as SerializedMethodDefinition);
}
#endregion Isinst

#region Ldftn

internal record Ldftn : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Ldloc_0,     // 4	0008	ldloc.0
        CilOpCodes.Callvirt,    // 5	0009	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance class [mscorlib]System.Reflection.MethodBase VM::ResolveMethod(int32)
        CilOpCodes.Stloc_1,     // 7	0013	stloc.1
        CilOpCodes.Ldarg_0,     // 8	0014	ldarg.0
        CilOpCodes.Newobj,      // 9	0015	newobj	instance void VMMethodBaseOperand::.ctor()
        CilOpCodes.Dup,         // 10	001A	dup
        CilOpCodes.Ldloc_1,     // 11	001B	ldloc.1
        CilOpCodes.Callvirt,    // 12	001C	callvirt	instance void VMMethodBaseOperand::method_4(class [mscorlib]System.Reflection.MethodBase)
        CilOpCodes.Callvirt,    // 13	0021	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 14	0026	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldftn;
    
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index = 0) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand is IMethodDescriptor
        {
            Signature.ReturnType.FullName: "System.Reflection.MethodBase"
        };
}
#endregion Ldftn

#region Ldvirtftn

internal record Ldvirtftn : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldloc_2,     // 52	0075	ldloc.2
        CilOpCodes.Ldloc_0,     // 53	0076	ldloc.0
        CilOpCodes.Callvirt,    // 54	0077	callvirt	instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
        CilOpCodes.Ldc_I4,      // 55	007C	ldc.i4	0x13036
        CilOpCodes.Ldnull,      // 56	0081	ldnull
        CilOpCodes.Ldc_I4_3,    // 57	0082	ldc.i4.3
        CilOpCodes.Ldloc_S,     // 58	0083	ldloc.s	V_6 (6)
        CilOpCodes.Ldnull,      // 59	0085	ldnull
        CilOpCodes.Callvirt,    // 60	0086	callvirt	instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string, valuetype [mscorlib]System.Reflection.BindingFlags, class [mscorlib]System.Reflection.Binder, valuetype [mscorlib]System.Reflection.CallingConventions, class [mscorlib]System.Type[], valuetype [mscorlib]System.Reflection.ParameterModifier[])
        CilOpCodes.Stloc_S,     // 61	008B	stloc.s	V_5 (5)
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldvirtftn;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeLdcI4OpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index = 0) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[index + 8].Operand is IMethodDescriptor
        {
            Signature.ReturnType.FullName: "System.Reflection.MethodInfo"
        };
}

#endregion

#region Sizeof

internal record Sizeof : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Call,        // 11	0017	call	int32 [mscorlib]System.Runtime.InteropServices.Marshal::SizeOf(class [mscorlib]System.Type)
        CilOpCodes.Newobj,      // 12	001C	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Callvirt,    // 13	0021	callvirt	instance void VM::PushStack(class VMOperandType)
    };

    public CilOpCode? CilOpCode => CilOpCodes.Sizeof;

    public bool MatchEntireBody => false;

    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var sizeOfCall = instructions[index].Operand as IMethodDescriptor;
        if (sizeOfCall?.FullName != "System.Int32 System.Runtime.InteropServices.Marshal::SizeOf(System.Type)")
            return false;

        var pushStackCall = instructions[index + 2].Operand as MethodDefinition;
        return PatternMatcher.MatchesPattern(new PushStackPattern(), pushStackCall);
    }
}

#endregion Sizeof

#region Break

internal record Break : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Call,        // 0	0000	call	void [mscorlib]System.Diagnostics.Debugger::Break()
        CilOpCodes.Ret          // 1	0005	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Break;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index].Operand as IMethodDescriptor)?.FullName ==
        "System.Void System.Diagnostics.Debugger::Break()";
}

#endregion Break