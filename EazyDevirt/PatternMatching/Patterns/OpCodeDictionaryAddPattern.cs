using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;

namespace EazyDevirt.PatternMatching.Patterns;

internal record OpCodeDictionaryAddPattern : IPattern
{
    /// <summary>
    /// Pattern for additions to the VM OpCode dictionary
    /// </summary>
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Dup,         // 2	000A	dup
        CilOpCodes.Ldarg_0,     // 3	000B	ldarg.0
        CilOpCodes.Ldfld,       // 4	000C	ldfld	valuetype VMOpCode VMOpCodeStructs::struct4_171
        CilOpCodes.Stloc_0,     // 5	0011	stloc.0
        CilOpCodes.Ldloca_S,    // 6	0012	ldloca.s	V_0 (0)
        CilOpCodes.Call,        // 7	0014	call	instance int32 VMOpCode::GetVMOpCodeType()
        CilOpCodes.Ldarg_0,     // 8	0019	ldarg.0
        CilOpCodes.Ldfld,       // 9	001A	ldfld	valuetype VMOpCode VMOpCodeStructs::struct4_171
        CilOpCodes.Ldnull,      // 10	001F	ldnull
        CilOpCodes.Ldftn,       // 11	0020	ldftn	void VM::smethod_121(class VM, class VMTypeAndVal)
        CilOpCodes.Newobj,      // 12	0026	newobj	instance void VM/Delegate21::.ctor(object, native int)
        CilOpCodes.Newobj,      // 13	002B	newobj	instance void VM/VMOperand::.ctor(valuetype VMOpCode, class VM/Delegate21)
        CilOpCodes.Callvirt,    // 14	0030	callvirt	instance void class [mscorlib]System.Collections.Generic.Dictionary`2<int32, valuetype VM/VMOperand>::Add(!0, !1)
                                // ...
    };
    
    public bool MatchEntireBody => false;
}