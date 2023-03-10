using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;
// ReSharper disable InconsistentNaming

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Conv_I

#region Conv_I8

internal record Conv_I8InnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_0,         // 75	00D0	ldloc.0
        CilOpCodes.Castclass,       // 76	00D1	castclass	VMEnumOperand
        CilOpCodes.Callvirt,        // 77	00D6	callvirt	instance class [System.Runtime]System.Enum VMEnumOperand::method_3()
        CilOpCodes.Call,            // 78	00DB	call	uint64 [mscorlib]System.Convert::ToUInt64(object)
        CilOpCodes.Conv_Ovf_I8_Un,  // 79	00E0	conv.ovf.i8.un
        CilOpCodes.Stloc_2,         // 80	00E1	stloc.2
    };

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[index + 3].Operand as SerializedMemberReference)?.FullName ==
        "System.UInt64 System.Convert::ToUInt64(System.Object)";
}


internal record Conv_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1 0001 ldc.i4.0
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::method_25(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Conv_I8;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I8InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Conv_Ovf_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1 0001 ldc.i4.1
        CilOpCodes.Callvirt,    // 2 0002 callvirt instance void VM::method_25(bool)
        CilOpCodes.Ret          // 3 0007 ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Conv_Ovf_I8;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new Conv_I8InnerPattern(),
        (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Conv_I8


#endregion Conv_I