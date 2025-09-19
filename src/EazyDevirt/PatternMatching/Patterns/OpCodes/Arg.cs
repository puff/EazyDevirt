using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Devirtualization;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

internal record ArgumentFieldPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Call,        // 18	002B	call	instance class VMOperandType[] VM::ResolveArgTypes(object[])
        CilOpCodes.Stfld,       // 19	0030	stfld	class VMOperandType[] VM::Arguments
    };

    public bool MatchEntireBody => false;

    public bool Verify(MethodDefinition method, int index = 0)
    {
        var instructions = method.CilMethodBody!.Instructions;
        var argumentField = instructions[index + 1].Operand as SerializedFieldDefinition;
        if (argumentField?.Signature!.FieldType.ElementType != ElementType.SzArray)
            return false;
        
        var resolveArgTypesMethod = instructions[index].Operand as SerializedMethodDefinition;
        return resolveArgTypesMethod?.Parameters.Count == 1 && resolveArgTypesMethod.Parameters[0].ParameterType.FullName == "System.Object[]";
    }
}

#region Ldarg
internal record PushArgumentToStackPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,         // 0	0000	ldarg.0
        CilOpCodes.Ldarg_0,         // 1	0001	ldarg.0
        CilOpCodes.Ldfld,           // 2	0002	ldfld	class VMOperandType[] VM::Arguments
        CilOpCodes.Ldarg_1,         // 3	0007	ldarg.1
        CilOpCodes.Ldelem,          // 4	0008	ldelem	VMOperandType
        CilOpCodes.Callvirt,        // 5	000D	callvirt	instance class VMOperandType VMOperandType::Clone()
        CilOpCodes.Call,            // 6	0012	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret              // 7	0017	ret
    };

    public bool Verify(MethodDefinition method, int index = 0) =>
        method.CilMethodBody!.Instructions[2].Operand as SerializedFieldDefinition ==
        DevirtualizationContext.Instance.VMArgumentsField;
}

internal record Ldarg : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Castclass,   // 2	0002	castclass	VMUShortOperand
        CilOpCodes.Callvirt,    // 3	0007	callvirt	instance uint16 VMUShortOperand::method_3()
        CilOpCodes.Callvirt,    // 4	000C	callvirt	instance void VM::PushArgumentToStack(int32)
        CilOpCodes.Ret          // 5	0011	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarg;
    
    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var getOperandMethod = instructions[3].Operand as SerializedMethodDefinition;
        if (getOperandMethod?.Signature!.ReturnType.FullName != "System.UInt16") return false;
        
        var pushArgumentToStackMethod = instructions[4].Operand as SerializedMethodDefinition;
        return PatternMatcher.MatchesPattern(new PushArgumentToStackPattern(), pushArgumentToStackMethod!);
    }
}

internal record Ldarg_S : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Castclass,   // 2	0002	castclass	VMByteOperand
        CilOpCodes.Callvirt,    // 3	0007	callvirt	instance uint8 VMByteOperand::method_3()
        CilOpCodes.Callvirt,    // 4	000C	callvirt	instance void VM::PushArgumentToStack(int32)
        CilOpCodes.Ret          // 5	0011	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarg_S;
    
    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var getOperandMethod = instructions[3].Operand as SerializedMethodDefinition;
        if (getOperandMethod?.Signature!.ReturnType.FullName != "System.Byte") return false;
        
        var pushArgumentToStackMethod = instructions[4].Operand as SerializedMethodDefinition;
        return PatternMatcher.MatchesPattern(new PushArgumentToStackPattern(), pushArgumentToStackMethod!);
    }
}
#endregion Ldarg

#region Ldarga
internal record Ldarga : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMUShortOperand
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Newobj,      // 4	0008	newobj	instance void Class44::.ctor()
        CilOpCodes.Dup,         // 5	000D	dup
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldfld,       // 7	000F	ldfld	class VMOperandType[] VM::Arguments
        CilOpCodes.Ldloc_0,     // 8	0014	ldloc.0
        CilOpCodes.Callvirt,    // 9	0015	callvirt	instance uint16 VMUShortOperand::method_3()
        CilOpCodes.Ldelem,      // 10	001A	ldelem	VMOperandType
        CilOpCodes.Callvirt,    // 11	001F	callvirt	instance void Class44::method_4(class VMOperandType)
        CilOpCodes.Callvirt,    // 12	0024	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 13	0029	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarga;
    
    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var argumentsField = instructions[7].Operand as SerializedFieldDefinition;
        if (argumentsField != DevirtualizationContext.Instance.VMArgumentsField) return false;
        
        var getOperandMethod = instructions[9].Operand as SerializedMethodDefinition;
        return getOperandMethod?.Signature!.ReturnType.FullName == "System.UInt16";
    }
}


internal record Ldarga_S : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMByteOperand
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Newobj,      // 4	0008	newobj	instance void Class44::.ctor()
        CilOpCodes.Dup,         // 5	000D	dup
        CilOpCodes.Ldarg_0,     // 6	000E	ldarg.0
        CilOpCodes.Ldfld,       // 7	000F	ldfld	class VMOperandType[] VM::Arguments
        CilOpCodes.Ldloc_0,     // 8	0014	ldloc.0
        CilOpCodes.Callvirt,    // 9	0015	callvirt	instance uint8 VMByteOperand::method_3()
        CilOpCodes.Ldelem,      // 10	001A	ldelem	VMOperandType
        CilOpCodes.Callvirt,    // 11	001F	callvirt	instance void Class44::method_4(class VMOperandType)
        CilOpCodes.Callvirt,    // 12	0024	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 13	0029	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarga_S;
    
    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var argumentsField = instructions[7].Operand as SerializedFieldDefinition;
        if (argumentsField != DevirtualizationContext.Instance.VMArgumentsField) return false;
        
        var getOperandMethod = instructions[9].Operand as SerializedMethodDefinition;
        return getOperandMethod?.Signature!.ReturnType.FullName == "System.Byte";
    }
}
#endregion Ldarga

#region Ldarg_C
internal record Ldarg_0 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_0,    // 1	0001	ldc.i4.0
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushArgumentToStack(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarg_0;

    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushArgumentToStackPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldarg_1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushArgumentToStack(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarg_1;

    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushArgumentToStackPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldarg_2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_2,    // 1	0001	ldc.i4.2
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushArgumentToStack(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarg_2;

    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushArgumentToStackPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}

internal record Ldarg_3 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_3,    // 1	0001	ldc.i4.3
        CilOpCodes.Callvirt,    // 2	0002	callvirt	instance void VM::PushArgumentToStack(int32)
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldarg_3;

    public bool Verify(MethodDefinition method, int index) => 
        PatternMatcher.MatchesPattern(new PushArgumentToStackPattern(), 
            (method.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!, index);
}
#endregion Ldarg_C

#region Starg
internal record StoreArgumentPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Ldfld,       // 4	0008	ldfld	class VMOperandType[] VM::Arguments
        CilOpCodes.Ldarg_1,     // 5	000D	ldarg.1
        CilOpCodes.Ldelem,      // 6	000E	ldelem	VMOperandType
        CilOpCodes.Ldloc_0,     // 7	0013	ldloc.0
        CilOpCodes.Callvirt,    // 8	0014	callvirt	instance class VMOperandType VMOperandType::ConvertAndSetOperandValue(class VMOperandType)
        CilOpCodes.Pop,         // 9	0019	pop
        CilOpCodes.Ret          // 10	001A	ret
    };

    public bool Verify(MethodDefinition method, int index = 0) =>
        method.CilMethodBody!.Instructions[4].Operand as SerializedFieldDefinition ==
        DevirtualizationContext.Instance.VMArgumentsField;
}

internal record Starg : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Castclass,   // 2	0002	castclass	VMUShortOperand
        CilOpCodes.Callvirt,    // 3	0007	callvirt	instance uint16 VMUShortOperand::method_3()
        CilOpCodes.Callvirt,    // 4	000C	callvirt	instance void VM::StoreArgument(int32)
        CilOpCodes.Ret          // 5	0011	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Starg;
    
    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var getOperandMethod = instructions[3].Operand as SerializedMethodDefinition;
        if (getOperandMethod?.Signature!.ReturnType.FullName != "System.UInt16") return false;
        
        var storeArgumentMethod = instructions[4].Operand as SerializedMethodDefinition;
        return PatternMatcher.MatchesPattern(new StoreArgumentPattern(), storeArgumentMethod!);
    }
}

internal record Starg_S : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldarg_1,     // 1	0001	ldarg.1
        CilOpCodes.Castclass,   // 2	0002	castclass	VMByteOperand
        CilOpCodes.Callvirt,    // 3	0007	callvirt	instance uint16 VMByteOperand::method_3()
        CilOpCodes.Callvirt,    // 4	000C	callvirt	instance void VM::StoreArgument(int32)
        CilOpCodes.Ret          // 5	0011	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Starg_S;
    
    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var getOperandMethod = instructions[3].Operand as SerializedMethodDefinition;
        if (getOperandMethod?.Signature!.ReturnType.FullName != "System.Byte") return false;
        
        var storeArgumentMethod = instructions[4].Operand as SerializedMethodDefinition;
        return PatternMatcher.MatchesPattern(new StoreArgumentPattern(), storeArgumentMethod!);
    }
}

#endregion Starg