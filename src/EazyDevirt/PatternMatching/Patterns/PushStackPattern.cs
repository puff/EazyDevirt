using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns;

internal record PushStackPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldarg_1,     // 0    0000   ldarg.1
        CilOpCodes.Brtrue_S,    // 1    0001   brtrue.s   6 (000F) ldarg.1
        CilOpCodes.Nop,         // 2    0003   nop
        CilOpCodes.Ldstr,       // 3    0004   ldstr      "obj"
        CilOpCodes.Newobj,      // 4    0009   newobj     instance void [System.Runtime]System.ArgumentNullException::.ctor(string)
        CilOpCodes.Throw,       // 5    000E   throw
        CilOpCodes.Ldarg_1,     // 6    000F   ldarg.1
        CilOpCodes.Callvirt,    // 7    0010   callvirt   instance class [System.Runtime]System.Type '\u000e'::'\u0002'()
        CilOpCodes.Ldnull,      // 8    0015   ldnull
        CilOpCodes.Call,        // 9    0016   call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type, class [System.Runtime]System.Type)
        CilOpCodes.Brfalse_S,   // 10   001B   brfalse.s  14 (0024) ldarg.1
        CilOpCodes.Ldarg_1,     // 11   001D   ldarg.1
                                // ...
    };
    
    public bool MatchEntireBody => false;
}