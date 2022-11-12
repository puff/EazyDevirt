using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;

namespace EazyDevirt.PatternMatching.Patterns;

public record InitializeVMStreamPattern : IPattern
{
    /// <summary>
    /// Pattern for the VM Resource Stream Initializer
    /// </summary>
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {

    };
    
    public bool Verify(MethodDefinition method)
    {
        throw new NotImplementedException();
    }
}