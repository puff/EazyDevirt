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

