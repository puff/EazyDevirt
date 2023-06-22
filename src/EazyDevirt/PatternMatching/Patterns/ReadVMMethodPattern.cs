using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns
{
    internal class ReadVMMethodPattern : IPattern
    {
        public IList<CilOpCode> Pattern => new List<CilOpCode>
        {
            CilOpCodes.Ldarg_0,     //34	0052	ldarg.0
            CilOpCodes.Ldarg_0,     //35	0053	ldarg.0
            CilOpCodes.Ldarg_0,     //36	0054	ldarg.0
            CilOpCodes.Ldfld,       //37	0055	ldfld	class EazBinaryReader VMRuntime::'\b'
            CilOpCodes.Call,        //38	005A	call	instance class '\b\u2001' VMRuntime::'\u0002'(class EazBinaryReader)
            CilOpCodes.Stfld        //39	005F	stfld	class '\b\u2001' VMRuntime::'\b\u2000'
                                    // ...
        };

        public bool MatchEntireBody => false;
        public bool Verify(CilInstructionCollection instructions, int index = 0)
        {
            var resolveVMTypeCodeMethod = instructions[index + 4].Operand as SerializedMethodDefinition;
            return resolveVMTypeCodeMethod?.Parameters.Count == 1;
        }
    }
}
