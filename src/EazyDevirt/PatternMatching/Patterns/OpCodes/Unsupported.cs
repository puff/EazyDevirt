using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

internal record UnsupportedPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Nop,         // 0	0000	nop
        CilOpCodes.Ldstr,       // 1	0001	ldstr	"<opcode> is not supported."
        CilOpCodes.Newobj,      // 2	0006	newobj	instance void [mscorlib]System.NotSupportedException::.ctor(string)
        CilOpCodes.Throw        // 3	000B	throw
    };

    protected static bool CheckUnsupportedString(string? opCode, string? operandString) => operandString?.ToLower() == $"{opCode} is not supported.";

    public bool Verify(CilInstructionCollection instructions, int index = 0) =>
        (instructions[2].Operand as SerializedMemberReference)!.FullName ==
        "System.Void System.NotSupportedException::.ctor(System.String)";
}

#region Refanyval

internal record Refanyval : UnsupportedPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Refanyval;

    public new bool Verify(CilInstructionCollection instructions, int index = 0) =>
        CheckUnsupportedString(CilOpCode?.Mnemonic!, instructions[1].Operand as string) && base.Verify(instructions);
}

#endregion Refanyval

#region Refanytype

internal record Refanytype : UnsupportedPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Refanytype;

    public new bool Verify(CilInstructionCollection instructions, int index = 0) =>
        CheckUnsupportedString(CilOpCode?.Mnemonic!, instructions[1].Operand as string) && base.Verify(instructions);
}

#endregion Refanytype

#region Cpobj

internal record Cpobj : UnsupportedPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Cpobj;

    public new bool Verify(CilInstructionCollection instructions, int index = 0) =>
        CheckUnsupportedString(CilOpCode?.Mnemonic!, instructions[1].Operand as string) && base.Verify(instructions);
}

#endregion Cpobj

#region Mkrefany

internal record Mkrefany : UnsupportedPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Mkrefany;

    public new bool Verify(CilInstructionCollection instructions, int index = 0) =>
        CheckUnsupportedString(CilOpCode?.Mnemonic!, instructions[1].Operand as string) && base.Verify(instructions);
}

#endregion Mkrefany

#region Arglist

internal record Arglist : UnsupportedPattern, IOpCodePattern
{
    public CilOpCode? CilOpCode => CilOpCodes.Arglist;

    public new bool Verify(CilInstructionCollection instructions, int index = 0) =>
        CheckUnsupportedString(CilOpCode?.Mnemonic!, instructions[1].Operand as string) && base.Verify(instructions);
}

#endregion Arglist