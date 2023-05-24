using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Devirtualization;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Call

internal record Call : IOpCodePattern
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
        CilOpCodes.Ldloc_1,     // 9	0015	ldloc.1
        CilOpCodes.Ldc_I4_0,    // 10	0016	ldc.i4.0
        CilOpCodes.Callvirt,    // 11	0017	callvirt	instance void VM::CallMethod(class [mscorlib]System.Reflection.MethodBase, bool)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Call;

    public bool Verify(VMOpCode vmOpCode, int index) => 
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[6].Operand as SerializedMethodDefinition)!
        .Signature!.ReturnType.FullName == "System.Reflection.MethodBase";
}

#endregion Call

#region Call

internal record Calli : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldfld,       // 9	0015	ldfld	class VMOperandType[] VM::Arguments
        CilOpCodes.Stloc_2,     // 10	001A	stloc.2
        CilOpCodes.Ldc_I4_0,    // 11	001B	ldc.i4.0
        CilOpCodes.Stloc_3,     // 12	001C	stloc.3
        CilOpCodes.Br_S,        // 13	001D	br.s	25 (0034) ldloc.3 
        CilOpCodes.Ldloc_2,     // 14	001F	ldloc.2
        CilOpCodes.Ldloc_3,     // 15	0020	ldloc.3
        CilOpCodes.Ldelem,      // 16	0021	ldelem	VMOperandType
        CilOpCodes.Stloc_S,     // 17	0026	stloc.s	V_4 (4)
        CilOpCodes.Ldarg_0,     // 18	0028	ldarg.0
        CilOpCodes.Ldloc_S,     // 19	0029	ldloc.s	V_4 (4)
        CilOpCodes.Callvirt,    // 20	002B	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ldloc_3,     // 21	0030	ldloc.3
        CilOpCodes.Ldc_I4_1,    // 22	0031	ldc.i4.1
        CilOpCodes.Add,         // 23	0032	add
        CilOpCodes.Stloc_3,     // 24	0033	stloc.3
        CilOpCodes.Ldloc_3,     // 25	0034	ldloc.3
        CilOpCodes.Ldloc_2,     // 26	0035	ldloc.2
        CilOpCodes.Ldlen,       // 27	0036	ldlen
        CilOpCodes.Conv_I4,     // 28	0037	conv.i4
        CilOpCodes.Blt_S,       // 29	0038	blt.s	14 (001F) ldloc.2 
        CilOpCodes.Ldarg_0,     // 30	003A	ldarg.0
        CilOpCodes.Ldloc_1,     // 31	003B	ldloc.1
        CilOpCodes.Ldc_I4_0,    // 32	003C	ldc.i4.0
        CilOpCodes.Callvirt,    // 33	003D	callvirt	instance void VM::CallMethod(class [mscorlib]System.Reflection.MethodBase, bool)
        CilOpCodes.Ret          // 34	0042	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Calli;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index)
    {
        var argumentsField = instructions[index].Operand as SerializedFieldDefinition;
        if (argumentsField != DevirtualizationContext.Instance.VMArgumentsField) 
            return false;

        var pushStackMethod = instructions[index + 11].Operand as SerializedMethodDefinition;
        return PatternMatcher.MatchesPattern(new PushStackPattern(), pushStackMethod);
    }
}

#endregion Calli

#region Callvirt

internal record CallvirtInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Stloc_S,     // 2	0006	stloc.s	V_6 (6)
        CilOpCodes.Ldarg_0,     // 3	0008	ldarg.0
        CilOpCodes.Ldloc_S,     // 4	0009	ldloc.s	V_6 (6)
        CilOpCodes.Callvirt,    // 5	000B	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Call,        // 6	0010	call	instance class [mscorlib]System.Reflection.MethodBase VM::ResolveMethod(int32)
        CilOpCodes.Stloc_0,     // 7	0015	stloc.0
        CilOpCodes.Ldarg_0,     // 8	0016	ldarg.0
        CilOpCodes.Ldfld,       // 9	0017	ldfld	class [mscorlib]System.Type VM::type_2
        CilOpCodes.Brfalse_S,   // 10	001C	brfalse.s	63 (0081) ldarg.0
    };

    public bool MatchEntireBody => false;

    public bool Verify(MethodDefinition method, int index) => 
        (method.CilMethodBody!.Instructions[6].Operand as SerializedMethodDefinition)!
        .Signature!.ReturnType.FullName == "System.Reflection.MethodBase";
}

internal record Callvirt : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::method_40(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Callvirt;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new CallvirtInnerPattern(), (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

#endregion Callvirt