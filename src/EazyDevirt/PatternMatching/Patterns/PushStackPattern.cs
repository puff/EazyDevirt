using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns;

internal record PushStackPattern : IPattern
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
        CilOpCodes.Callvirt,    // 7	0010	callvirt	instance class [mscorlib]System.Type VMOperandType::GetOperandType()
        CilOpCodes.Brfalse_S,   // 8	0015	brfalse.s	12 (001E) ldarg.1 
        CilOpCodes.Ldarg_1,     // 9	0017	ldarg.1
        CilOpCodes.Stloc_0,     // 10	0018	stloc.0
        CilOpCodes.Br,          // 11	0019	br	168 (0208) ldarg.0 
                                // ...
    };
    
    public bool MatchEntireBody => false;
}