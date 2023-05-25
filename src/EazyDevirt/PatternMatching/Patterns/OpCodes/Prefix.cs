using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Constrained

internal record Constrained : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldarg_0,     // 5	000D	ldarg.0
        CilOpCodes.Ldloc_0,     // 6	000E	ldloc.0
        CilOpCodes.Ldc_I4_1,    // 7	000F	ldc.i4.1
        CilOpCodes.Callvirt,    // 8	0010	callvirt	instance class [mscorlib]System.Type VM::ResolveVMTypeCodeCache(int32, bool)
        CilOpCodes.Stfld,       // 9	0015	stfld	class [mscorlib]System.Type VM::ConstrainedType
        CilOpCodes.Ret,         // 10	001A	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Constrained;
    
    public bool Verify(CilInstructionCollection instructions, int index = 0)
    {
        var constrainedTypeField = instructions[9].Operand as FieldDefinition;
        return constrainedTypeField?.Signature?.FieldType.FullName == "System.Type";
    }
}

#endregion Constrained

#region Volatile

internal record Volatile : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Call,        // 0	0000	call	void [mscorlib]System.Threading.Thread::MemoryBarrier()
        CilOpCodes.Ret          // 1	0005	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Volatile;

    public bool AllowMultiple => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index].Operand as IMethodDescriptor)?.FullName ==
        "System.Void System.Threading.Thread::MemoryBarrier()";
}

#endregion Volatile