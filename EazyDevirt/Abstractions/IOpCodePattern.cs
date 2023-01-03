using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Architecture;

namespace EazyDevirt.Abstractions;

internal interface IOpCodePattern : IPattern
{
    CilOpCode CilOpCode { get; }
    SpecialOpCode SpecialOpCode { get; }
    
    /// <summary>
    /// Whether the pattern can translate to CIL opcodes or is a special vm action
    /// </summary>
    bool IsSpecial { get; }
}