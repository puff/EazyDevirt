using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

#region Leave

internal record LeaveProtectedPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 27	0051	ldarg.0
        CilOpCodes.Call,        // 28	0052	call	instance void VM::ClearStack()
        CilOpCodes.Ldarg_0,     // 29	0057	ldarg.0
        CilOpCodes.Ldloca_S,    // 30	0058	ldloca.s	V_0 (0)
        CilOpCodes.Call,        // 31	005A	call	instance uint32 VM/Struct13::method_0()
        CilOpCodes.Call,        // 32	005F	call	instance void VM::SetBranchIndex(uint32)
        CilOpCodes.Ret          // 33	0064	ret
    };

    public bool MatchEntireBody => false;

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        PatternMatcher.MatchesPattern(new SetBranchIndexPattern(),
            instructions[index + 5].Operand as SerializedMethodDefinition);
}

internal record Leave : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMUIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance uint32 VMUIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldnull,      // 5	000D	ldnull
        CilOpCodes.Ldloc_0,     // 6	000E	ldloc.0
        CilOpCodes.Callvirt,    // 7	000F	callvirt	instance void VM::method_34(object, uint32)
        CilOpCodes.Ret          // 8	0014	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Leave;
    
    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var method = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[7].Operand as SerializedMethodDefinition;
        return (method?.CilMethodBody?.Instructions.Where(x => x.OpCode.Code is CilCode.Call or CilCode.Callvirt)
            .Any(x => PatternMatcher.MatchesPattern(new LeaveProtectedPattern(),
                x.Operand as SerializedMethodDefinition))).GetValueOrDefault();
    }
}
#endregion Leave

#region Endfinally

internal record Endfinally : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance void VM::LeaveProtected()
        CilOpCodes.Ret          // 2	0006	ret
    };
    
    public CilOpCode? CilOpCode => CilOpCodes.Endfinally;

    public bool Verify(VMOpCode vmOpCode, int index = 0) => PatternMatcher.MatchesPattern(new LeaveProtectedPattern(),
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand as SerializedMethodDefinition);
}
#endregion Endfinally

#region Throw

internal record ThrowExceptionPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Isinst,      // 1	0001	isinst	[mscorlib]System.Exception
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldloc_0,     // 3	0007	ldloc.0
        CilOpCodes.Brfalse_S,   // 4	0008	brfalse.s	7 (0010) ldarg.1 
        CilOpCodes.Ldloc_0,     // 5	000A	ldloc.0
        CilOpCodes.Call,        // 6	000B	call	void VM::smethod_42(class [mscorlib]System.Exception)
        CilOpCodes.Ldarg_1,     // 7	0010	ldarg.1
        CilOpCodes.Call,        // 8	0011	call	void VM::smethod_200(object)
        CilOpCodes.Ret          // 9	0016	ret
    };

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        instructions[1].Operand is ITypeDescriptor
        {
            FullName: "System.Exception"
        };
}

internal record Throw : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Ldloc_0,     // 4	0008	ldloc.0
        CilOpCodes.Callvirt,    // 5	0009	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance void VM::ThrowException(object)
        CilOpCodes.Ret          // 7	0013	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Throw;

    public bool Verify(VMOpCode vmOpCode, int index = 0) =>
        PatternMatcher.MatchesPattern(new ThrowExceptionPattern(),
            vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[6].Operand as SerializedMethodDefinition);
}

#endregion Throw

#region Rethrow

internal record Rethrow : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldfld,       // 1	0001	ldfld	object VM::CurrentException
        CilOpCodes.Brtrue_S,    // 2	0006	brtrue.s	5 (000E) ldarg.0 
        CilOpCodes.Newobj,      // 3	0008	newobj	instance void [mscorlib]System.InvalidOperationException::.ctor()
        CilOpCodes.Throw,       // 4	000D	throw
        CilOpCodes.Ldarg_0,     // 5	000E	ldarg.0
        CilOpCodes.Ldarg_0,     // 6	000F	ldarg.0
        CilOpCodes.Ldfld,       // 7	0010	ldfld	object VM::CurrentException
        CilOpCodes.Callvirt,    // 8	0015	callvirt	instance void VM::ThrowException(object)
        CilOpCodes.Ret          // 9	001A	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Rethrow;

    public bool Verify(VMOpCode vmOpCode, int index = 0) =>
        PatternMatcher.MatchesPattern(new ThrowExceptionPattern(),
            vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions[8].Operand as SerializedMethodDefinition);
}
#endregion Rethrow