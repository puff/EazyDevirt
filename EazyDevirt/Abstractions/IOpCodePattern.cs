using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Abstractions;

internal interface IOpCodePattern : IPattern
{
    CilOpCode CilOpCode { get; }
    SpecialOpCode SpecialOpCode { get; }
    
    /// <summary>
    /// Whether the pattern can translate to CIL opcodes or is a special vm action
    /// </summary>
    bool IsSpecial { get; }

    bool Verify(VMOpCode vmOpCode, int index = 0) => Verify(vmOpCode.SerializedDelegateMethod, index);
}