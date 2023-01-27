using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;

namespace EazyDevirt.PatternMatching.Patterns;

internal record OpCodeCtorPattern : IPattern
{
    /// <summary>
    /// Pattern for VM OpCode field initializations in the .ctor method of the VM OpCode container type.
    /// </summary>
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 735	0A34	ldarg.0
        CilOpCodes.Ldc_I4,      // 736	0A35	ldc.i4	0x375E72C0
        CilOpCodes.Ldc_I4_S,    // 737	0A3A	ldc.i4.s	9
        CilOpCodes.Newobj,      // 738	0A3C	newobj	instance void VMOpCode::.ctor(int32, uint8)
        CilOpCodes.Stfld        // 739	0A41	stfld	valuetype VMOpCode VMOpCodeStructs::struct4_185
    };

    public bool InterchangeLdcOpCodes => true;
    
    public bool MatchEntireBody => false;
}