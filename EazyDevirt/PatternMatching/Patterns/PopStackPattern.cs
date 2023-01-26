using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;

namespace EazyDevirt.PatternMatching.Patterns;

internal record PopStackPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldarg_0,     // 14	001F	ldarg.0
        CilOpCodes.Ldfld,       // 15	0020	ldfld	class [System]System.Collections.Generic.Stack`1<class Class20> VM::stack_2
        CilOpCodes.Callvirt,    // 16	0025	callvirt	instance !0 class [System]System.Collections.Generic.Stack`1<class Class20>::Pop()
        CilOpCodes.Ret          // 17	002A	ret
    };
    
    public bool MatchEntireBody => false;

    public bool Verify(MethodDefinition method, int index)
    {
        var instructions = method.CilMethodBody?.Instructions;
        return method.Parameters.Count == 0 && (instructions![index + 2].Operand as SerializedMemberReference)?.Name == "Pop";
    }
}