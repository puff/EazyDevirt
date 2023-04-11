using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Ldc_R
internal record Ldc_R8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_R8;
    
    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.CilOperandType == CilOperandType.InlineR && PatternMatcher.MatchesPattern(new PushStackPattern(), 
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Ldc_R4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_R4;
    
    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.CilOperandType == CilOperandType.ShortInlineR && PatternMatcher.MatchesPattern(new PushStackPattern(), 
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Ldc_R

#region Ldc_I
internal record Ldc_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I8;
    
    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.CilOperandType == CilOperandType.InlineI8 && PatternMatcher.MatchesPattern(new PushStackPattern(), 
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Ldc_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.CilOperandType == CilOperandType.InlineI && PatternMatcher.MatchesPattern(new PushStackPattern(), 
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}

internal record Ldc_I4_S : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_S;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.CilOperandType == CilOperandType.ShortInlineI && PatternMatcher.MatchesPattern(new PushStackPattern(), 
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!);
}
#endregion Ldc_I

#region Ldc_I4_C
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
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_0;

    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_1;

    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_2,    // 1	0001	ldc.i4.2
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_2;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_3 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_3,    // 1	0001	ldc.i4.3
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_3;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_4,    // 1	0001	ldc.i4.4
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_4;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_5 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_5,    // 1	0001	ldc.i4.5
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_5;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_6 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_6,    // 1	0001	ldc.i4.6
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_6;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_7 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_7,    // 1	0001	ldc.i4.7
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_7;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_8,    // 1	0001	ldc.i4.8
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_8;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldc_I4_M1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_M1,   // 1	0001	ldc.i4.m1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushLdcI4COperand(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldc_I4_M1;
    
    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushLdcI4COperandPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}
#endregion Ldc_I4_C