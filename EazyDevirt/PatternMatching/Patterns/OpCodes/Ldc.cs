using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

// TODO: Maybe move this and PushLdcI4COperandPattern to their own files if other types of opcodes need them
internal record PushStackLdcOperandPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Brtrue_S,    // 1	0001	brtrue.s	6 (000F) ldarg.1 
        CilOpCodes.Nop,         // 2	0003	nop
        CilOpCodes.Ldstr,       // 3	0004	ldstr	"obj"
        CilOpCodes.Newobj,      // 4	0009	newobj	instance void [mscorlib]System.ArgumentNullException::.ctor(string)
        CilOpCodes.Throw,       // 5	000E	throw
        CilOpCodes.Ldarg_1,     // 6	000F	ldarg.1
        CilOpCodes.Callvirt,    // 7	0010	callvirt	instance class [mscorlib]System.Type VMOperandType::method_1()
        CilOpCodes.Brfalse_S,   // 8	0015	brfalse.s	12 (001E) ldarg.1 
        CilOpCodes.Ldarg_1,     // 9	0017	ldarg.1
        CilOpCodes.Stloc_0,     // 10	0018	stloc.0
        CilOpCodes.Br,          // 11	0019	br	168 (0208) ldarg.0 
                                // ...
    };
}

#region Ldc_I4
internal record Ldc_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        // TODO: Implement pattern matching for virtual operand type
        return vmOpCode.VirtualOperandType == 12 && PatternMatcher.MatchesPattern(new PushStackLdcOperandPattern(), 
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
    }
}

/// <summary>
/// Used in the Ldc_I4_C delegate methods
/// </summary>
internal record PushLdcI4COperandPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Newobj,      // 2	0002	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Call,        // 3	0007	call	instance void VM::PushStack(class VMTypeAndVal)
        CilOpCodes.Ret          // 4	000C	ret
    };

    public bool Verify(MethodDefinition method, int index = 0) => method.Parameters.Count == 1 &&
                                                                  method.Parameters[0].ParameterType.FullName == "System.Int32";
}

internal record Ldc_I4_0 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1	0001	ldc.i4.0
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_0;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;

    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_1;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;

    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_2,    // 1	0001	ldc.i4.2
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_2;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_3 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_3,    // 1	0001	ldc.i4.3
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_3;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_4,    // 1	0001	ldc.i4.4
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_4;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_5 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_5,    // 1	0001	ldc.i4.5
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_5;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_6 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_6,    // 1	0001	ldc.i4.6
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_6;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_7 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_7,    // 1	0001	ldc.i4.7
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_7;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_8,    // 1	0001	ldc.i4.8
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_8;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_M1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_M1,   // 1	0001	ldc.i4.m1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperandPattern(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4_M1;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;
    
    public bool Verify(MethodDefinition method, int index) => PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}
#endregion Ldc_I4