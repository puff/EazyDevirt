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