using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns;

internal record TypeFieldPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldtoken,     // 18	005A	ldtoken	[mscorlib]System.IntPtr
        CilOpCodes.Call,        // 19	005F	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Stsfld,      // 20	0064	stsfld	class [mscorlib]System.Type VM::type_5
    };
    
    public bool MatchEntireBody => false;
}