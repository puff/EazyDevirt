using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region EazCall

internal record EazCall : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldloc_0,     // 4	000C	ldloc.0
        CilOpCodes.Ldc_I4,      // 5	000D	ldc.i4	-0x80000000
        CilOpCodes.And,         // 6	0012	and
        CilOpCodes.Ldc_I4_0,    // 7	0013	ldc.i4.0
        CilOpCodes.Cgt_Un,      // 8	0014	cgt.un
        CilOpCodes.Ldloc_0,     // 9	0016	ldloc.0
        CilOpCodes.Ldc_I4,      // 10	0017	ldc.i4	0x40000000
    };

    public CilOpCode? CilOpCode => CilOpCodes.Call;
    public SpecialOpCodes? SpecialOpCode => SpecialOpCodes.EazCall;
    
    public bool IsSpecial => true;
    public bool MatchEntireBody => false;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 5].Operand as int? == -0x80000000;
}
#endregion EazCall

#region Homomorphic Encryption

internal record StartHomomorphic : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	class Class94 VM::smethod_135(class VM)
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Ldfld,       // 4	0008	ldfld	class VMBinaryReader VM::instructionsBinaryReader
        CilOpCodes.Callvirt,    // 5	000D	callvirt	instance class StreamBase VMBinaryReader::GetStream()
        CilOpCodes.Stloc_S,     // 6	0012	stloc.s	V_4 (4)
    };

    public CilOpCode? CilOpCode => null;
    public SpecialOpCodes? SpecialOpCode => SpecialOpCodes.StartHomomorphic;
    
    public bool IsSpecial => true;
    public bool MatchEntireBody => false;

    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        ((vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 5].Operand as IMethodDefOrRef)?.Signature?.ReturnType?.Resolve()?.IsAbstract).GetValueOrDefault();
}

internal record EndHomomorphic : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_1,     // 14	0028	ldloc.1
        CilOpCodes.Callvirt,    // 15	0029	callvirt	instance int32 class [System]System.Collections.Generic.Stack`1<class '#=q9lm1K5MbvbOloCUh$L2kujlbpJIte9dF4zmzVcvK$yo='/'#=zbMR0slg='>::get_Count()
        CilOpCodes.Ldc_I4_2,    // 16	002E	ldc.i4.2
        CilOpCodes.Bge_S,       // 17	002F	bge.s	20 (0037) ldloc.1 
        CilOpCodes.Newobj,      // 18	0031	newobj	instance void [mscorlib]System.InvalidOperationException::.ctor()
        CilOpCodes.Throw,       // 19	0036	throw
        CilOpCodes.Ldloc_1,     // 20	0037	ldloc.1
        CilOpCodes.Callvirt,    // 21	0038	callvirt	instance !0 class [System]System.Collections.Generic.Stack`1<class VM/Class85>::Pop()
    };

    public CilOpCode? CilOpCode => null;
    public SpecialOpCodes? SpecialOpCode => SpecialOpCodes.EndHomomorphic;
    
    public bool IsSpecial => true;
    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 1].Operand is IMethodDefOrRef methodDefOrRef
        && methodDefOrRef.Name == "get_Count"
        && (methodDefOrRef.DeclaringType?.FullName.StartsWith("System.Collections.Generic.Stack")).GetValueOrDefault();
}

#endregion

#region NoBody

internal record NoBody : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ret           // 0	0000	ret
    };

    public CilOpCode? CilOpCode => null;
    public SpecialOpCodes? SpecialOpCode => SpecialOpCodes.NoBody;
    
    public bool IsSpecial => true;
    public bool AllowMultiple => true;

    public bool Verify(VMOpCode vmOpCode, int index) => vmOpCode.CilOperandType == CilOperandType.InlineNone;
}
#endregion NoBody