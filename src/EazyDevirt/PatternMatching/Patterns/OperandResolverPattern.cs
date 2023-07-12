using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns
{
    internal class OperandResolverPattern : IPattern
    {
        public IList<CilOpCode> Pattern => new List<CilOpCode>()
        {
            CilOpCodes.Callvirt,    //17	002E	callvirt	instance int64 '\u000e\u2004'::'\u000e\u2004\u2000\u2000\u2009\u2009\u0002'(int64, int32)
            CilOpCodes.Pop,         //18	0033	pop
            CilOpCodes.Newobj,      //19	0034	newobj	instance void '\u000e\u2000'::.ctor()
            CilOpCodes.Stloc_1,     //20	0039	stloc.1
            CilOpCodes.Ldloc,       //21	003A	ldloc.1
            CilOpCodes.Ldarg_0,     //22	003B	ldarg.0
            CilOpCodes.Ldfld,       //23	003C	ldfld	class '\u000f\u2002' '\b\u2008'::'\b\u2000'
            CilOpCodes.Callvirt,    //24	0041	callvirt	instance uint8 '\u000f\u2002'::'\u0002'()
            CilOpCodes.Callvirt,    //25	0046	callvirt	instance void '\u000e\u2000'::'\u0002'(uint8)
            CilOpCodes.Ldloc_1,     //26	004B	ldloc.1
            CilOpCodes.Callvirt     //27	004C	callvirt	instance uint8 '\u000e\u2000'::'\u0002'()
                                    // ....
        };

        public bool InterchangeStlocOpCodes => true;
        public bool InterchangeLdlocOpCodes => true;
        public bool MatchEntireBody => false;
    }
}
