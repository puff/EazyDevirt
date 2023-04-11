using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns;

internal record PeekStackPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldfld,       // 1	0001	ldfld	class VMOperandType VM::class20_0
        CilOpCodes.Dup,         // 2	0006	dup
        CilOpCodes.Brtrue_S,    // 3	0007	brtrue.s	8 (0015) ret 
        CilOpCodes.Pop,         // 4	0009	pop
        CilOpCodes.Ldarg_0,     // 5	000A	ldarg.0
        CilOpCodes.Ldfld,       // 6	000B	ldfld	class [System]System.Collections.Generic.Stack`1<class VMOperandType> VM::stack_2
        CilOpCodes.Callvirt,    // 7	0010	callvirt	instance !0 class [System]System.Collections.Generic.Stack`1<class VMOperandType>::Peek()
        CilOpCodes.Ret          // 8	0015	ret
    };

    public bool Verify(MethodDefinition method, int index = 0) => (method.CilMethodBody!.Instructions[7].Operand as SerializedMemberReference)?.Name == "Peek";
}