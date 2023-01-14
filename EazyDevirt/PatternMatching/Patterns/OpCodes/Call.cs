using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

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
        CilOpCodes.Callvirt,    // 11	0017	callvirt	instance void VM::method_50(class [mscorlib]System.Reflection.MethodBase, bool)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Call;

    public bool Verify(VMOpCode vmOpCode, int index) => 
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[6].Operand as SerializedMethodDefinition)!
        .Signature!.ReturnType.FullName == "System.Reflection.MethodBase";
}

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
                                // ...
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

    public CilOpCode CilOpCode => CilOpCodes.Callvirt;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new CallvirtInnerPattern(), (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}