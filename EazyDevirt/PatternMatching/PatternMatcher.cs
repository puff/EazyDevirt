using System.Text.RegularExpressions;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Architecture;

namespace EazyDevirt.PatternMatching;

public class PatternMatcher
{
    public PatternMatcher()
    {
        VMPatterns = new List<IVMPattern>();
        foreach (var type in typeof(PatternMatcher).Assembly.GetTypes())
            if (type.GetInterface(nameof(IVMPattern)) != null)
                if (Activator.CreateInstance(type) is IVMPattern instance)
                    VMPatterns.Add(instance);
    }
    
    private List<IVMPattern> VMPatterns { get; }

    public VMOpCode FindVMOpCode(MethodDefinition method, int index = 0)
    {
        return VMOpCode.Nop;
    }

    /// <summary>
    /// Checks if pattern matches a method's instructions body.
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against.</param>
    /// <param name="method">Method to match body against</param>
    /// <param name="index">Index of method's instruction body to start matching at.</param>
    /// <returns>Whether the pattern matches method's instruction body</returns>
    public static bool MatchesPattern(IPattern pattern, MethodDefinition method, int index = 0)
    {
        if (!method.HasMethodBody) return false;
        var instructions = method.CilMethodBody!.Instructions;
        
        var pat = pattern.Pattern;
        if (index + pat.Count > instructions.Count) return false;

        return !pat.Where((t, i) => t != CilOpCodes.Nop && (instructions[i + index].OpCode != t 
                                                            || !pattern.Verify(method))).Any();
    }
}