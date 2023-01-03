using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;

namespace EazyDevirt.PatternMatching.Patterns;

internal record Ldc_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldc_I4;
    public SpecialOpCode SpecialOpCode { get; }
    public bool IsSpecial => false;

    public bool Verify(CilInstructionCollection instructions)
    {
        return true;
    }
}