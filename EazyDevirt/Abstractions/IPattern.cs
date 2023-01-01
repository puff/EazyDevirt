using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace EazyDevirt.Abstractions;

internal interface IPattern
{
    /// <summary>
    /// Pattern of CilOpCodes in order of top to bottom to check against CIL instruction bodies.
    /// </summary>
    IList<CilOpCode> Pattern { get; }

    /// <summary>
    /// Whether this pattern allows to interchange Ldc OpCodes like Ldc_I4 and Ldc_I4_8
    /// </summary>
    bool InterchangeLdcOpCodes => false;

    /// <summary>
    /// Additional verification to be sure the match is valid.
    /// </summary>
    /// <param name="method">Method to match Pattern against</param>
    /// <returns>Whether verification is successful</returns>
    bool Verify(MethodDefinition method) => Verify(method.CilMethodBody!.Instructions);

    bool Verify(CilInstructionCollection instructions) => true;
}