using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Architecture;

namespace EazyDevirt.Abstractions;

public interface IPattern
{
    /// <summary>
    /// Pattern of CilOpCodes in order of top to bottom to check against CIL instruction bodies.
    /// </summary>
    IList<CilOpCode> Pattern { get; }

    /// <summary>
    /// Additional verification to be sure the match is valid.
    /// </summary>
    /// <param name="method">Method to match Pattern against</param>
    /// <returns>Whether verification is successful</returns>
    bool Verify(MethodDefinition method);
}