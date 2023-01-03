using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;

namespace EazyDevirt.PatternMatching;

internal class PatternMatcher
{
    public PatternMatcher()
    {
        OpCodes = new Dictionary<int, VMOpCode>();
        OpCodePatterns = new List<IOpCodePattern>();
        foreach (var type in typeof(PatternMatcher).Assembly.GetTypes())
            if (type.GetInterface(nameof(IOpCodePattern)) != null)
                if (Activator.CreateInstance(type) is IOpCodePattern instance)
                    OpCodePatterns.Add(instance);
    }
    
    private Dictionary<int, VMOpCode> OpCodes { get; }
    private List<IOpCodePattern> OpCodePatterns { get; }
    
    public void SetOpCodeValue(int value, VMOpCode opCode) => OpCodes[value] = opCode;

    public VMOpCode GetOpCodeValue(int value) => OpCodes.TryGetValue(value, out var opc) ? opc : new VMOpCode(null!, null!);


    /// <summary>
    /// Checks if pattern matches a method's instructions body
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against</param>
    /// <param name="method">Method to match body against</param>
    /// <param name="index">Index of method's instruction body to start matching at</param>
    /// <returns>Whether the pattern matches method's instruction body</returns>
    public static bool MatchesPattern(IPattern pattern, MethodDefinition method, int index = 0) => method.HasMethodBody && MatchesPattern(pattern, method.CilMethodBody!.Instructions, index);

    /// <summary>
    /// Checks if pattern matches a method's instructions body.
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against.</param>
    /// <param name="instructions">Instructions to match body against</param>
    /// <param name="index">Index of the instructions collection to start matching at</param>
    /// <returns>Whether the pattern matches the given instructions</returns>
    public static bool MatchesPattern(IPattern pattern, CilInstructionCollection instructions, int index = 0)
    {
        var pat = pattern.Pattern;
        if (index + pat.Count > instructions.Count) return false;
        
        return !pat.Where((t, i) => t != CilOpCodes.Nop && ((instructions[i + index].OpCode != t && (!instructions[i + index].IsLdcI4() || !pattern.InterchangeLdcOpCodes))
                                                            || !pattern.Verify(instructions))).Any();
    }

    /// <summary>
    /// Gets all matching instruction sets in a method's instructions body.
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against.</param>
    /// <param name="method">Method to match body against</param>
    /// <param name="index">Index of method's instruction body to start matching at.</param>
    /// <returns>List of matching instruction sets</returns>
    public static List<CilInstruction[]> GetAllMatchingInstructions(IPattern pattern, MethodDefinition method, int index = 0)
    {
        if (!method.HasMethodBody) return new List<CilInstruction[]>();
        var instructions = method.CilMethodBody!.Instructions;
        
        var pat = pattern.Pattern;
        if (index + pat.Count > instructions.Count) return new List<CilInstruction[]>();

        var matchingInstructions = new List<CilInstruction[]>();
        for (var i = index; i < instructions.Count; i += pat.Count)
        {
            var current = new List<CilInstruction>();

            for(int j = i, k = 0; j < instructions.Count && k < pat.Count; j++, k++)
            {
                var instruction = instructions[j];
                if (instruction.OpCode != pat[k] && (!instruction.IsLdcI4() || !pattern.InterchangeLdcOpCodes))
                    break;
                current.Add(instructions[j]);
            }

            if (current.Count == pat.Count)
                matchingInstructions.Add(current.ToArray());
        }

        return matchingInstructions;
    }
}