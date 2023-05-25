using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Devirtualization;
// ReSharper disable InconsistentNaming

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Ldind

internal record LdindInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Castclass,   // 2	0006	castclass	Class38
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldarg_0,     // 5	000D	ldarg.0
        CilOpCodes.Ldloc_0,     // 6	000E	ldloc.0
        CilOpCodes.Call,        // 7	000F	call	instance class VMOperandType VM::method_85(class Class38)
        CilOpCodes.Callvirt,    // 8	0014	callvirt	instance object VMOperandType::GetOperandValue()
        CilOpCodes.Ldarg_1,     // 9	0019	ldarg.1
        CilOpCodes.Call,        // 10	001A	call	class VMOperandType VMOperandType::ConvertToVMOperand(object, class [mscorlib]System.Type)
        CilOpCodes.Call,        // 11	001F	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 12	0024	ret
    };

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(MethodDefinition method, int index = 0) =>
        method.Parameters[0].ParameterType.FullName == "System.Type";
}

internal record Ldind_Ref : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldsfld,      // 1	0001	ldsfld	class [mscorlib]System.Type TypeHelpers::type_object
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 3	000B	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_Ref;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedFieldDefinition fieldDef
        && DevirtualizationContext.Instance.VMTypeFields.All(x =>
            x.Key.MetadataToken != fieldDef.MetadataToken);
}

#region Ldind_I

internal record Ldind_I : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldsfld,      // 1	0001	ldsfld	class [mscorlib]System.Type VM::type_5
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 3	000B	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_I;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedFieldDefinition fieldDef
        && DevirtualizationContext.Instance.VMTypeFields.Any(x =>
            x.Key.MetadataToken == fieldDef.MetadataToken && x.Value.FullName is "System.IntPtr");
}

internal record Ldind_I1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.SByte
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_I1;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.SByte"
        };
}

internal record Ldind_I2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Int16
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_I2;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.Int16"
        };
}

internal record Ldind_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Int32
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_I4;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.Int32"
        };
}

internal record Ldind_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Int64
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_I8;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.Int64"
        };
}
#endregion Ldind_I

#region Ldind_R

internal record Ldind_R4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Single
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_R4;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.Single"
        };
}

internal record Ldind_R8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Double
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_R8;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.Double"
        };
}
#endregion Ldind_R

#region Ldind_U

internal record Ldind_U1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Byte
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_U1;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.Byte"
        };
}

internal record Ldind_U2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.UInt16
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_U2;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.UInt16"
        };
}

internal record Ldind_U4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.UInt32
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldind_U4;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is ITypeDefOrRef
        {
            FullName: "System.UInt32"
        };
}
#endregion Ldind_U

#endregion Ldind

#region Ldobj

internal record Ldobj : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 9	0015	ldarg.0
        CilOpCodes.Ldloc_1,     // 10	0016	ldloc.1
        CilOpCodes.Callvirt,    // 11	0017	callvirt	instance void VM::LdindInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldobj;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdindInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 2].Operand as SerializedMethodDefinition)!);
}

#endregion Ldobj

// TODO: Handle stind opcodes
// The stind (and stobj) opcode handlers all use the same method, however the method has no parameters, therefore having no way of
// differentiating them through pattern matching (without matching each VMOperandType, because that sounds like a lot of effort).
// Due to this, we cannot use pattern matching for each stind opcode.
// Instead of pattern matching them, once the opcode devirtualization has finished, we should iterate through the
// devirtualized instructions and determine the stind type from stack analysis.
// This can probably be done with Echo, using a DFG or emulation.
#region Stind

internal record StindInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0 0000 ldarg.0
        CilOpCodes.Call,        // 1 0001 call instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2 0006 stloc.0
        CilOpCodes.Ldarg_0,     // 3 0007 ldarg.0
        CilOpCodes.Call,        // 4 0008 call instance class VMOperandType VM::PopStack()
        CilOpCodes.Castclass,   // 5 000D castclass Class38
        CilOpCodes.Stloc_1,     // 6 0012 stloc.1
        CilOpCodes.Ldarg_0,     // 7 0013 ldarg.0
        CilOpCodes.Ldloc_1,     // 8 0014 ldloc.1
        CilOpCodes.Ldloc_0,     // 9 0015 ldloc.0
        CilOpCodes.Call,        // 10 0016 call instance void VM::method_57(class Class38,  class VMOperandType)
        CilOpCodes.Ret          // 11 001B ret
    };
}

internal record Stind_I : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance void VM::StindInner()
        CilOpCodes.Ret          // 2	0006	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Stind_I;

    public bool AllowMultiple => true;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        PatternMatcher.MatchesPattern(new StindInnerPattern(),
            (instructions[index + 1].Operand as SerializedMethodDefinition)!);
}
#endregion