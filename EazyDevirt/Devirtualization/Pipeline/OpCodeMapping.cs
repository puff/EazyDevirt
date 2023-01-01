using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;
using EazyDevirt.PatternMatching;
using EazyDevirt.PatternMatching.Patterns;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class OpCodeMapping : Stage
{
    public override bool Run()
    {
        if (!Init()) return false;

        var dictMethod = FindOpCodeMethod();

        var dictAddOperations =
            PatternMatcher.GetAllMatchingInstructions(new OpCodeDictionaryAddPattern(), dictMethod, 2);

        var vmOpCodes = new List<VMOpCode>(dictAddOperations.Count);
        TypeDefinition containerType = null!;
        foreach (var op in dictAddOperations)
        {
            var instructionField = op[2].Operand as SerializedFieldDefinition;
            var instructionField2 = op[7].Operand as SerializedFieldDefinition;
            var opCodeDelegate = op[9].Operand as SerializedMethodDefinition;
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            containerType ??= instructionField!.DeclaringType!;

            // Thread.MemoryBarrier() opcode
            // in example, this is the 0x040001D2 and 0x040001C0 opcode structs.
            if (instructionField != instructionField2)
                vmOpCodes.Add(new VMOpCode(instructionField2!, opCodeDelegate!));

            vmOpCodes.Add(new VMOpCode(instructionField!, opCodeDelegate!));
        }
        
        if (containerType == null)
            throw new Exception("VM OpCode container type cannot be null");

        var containerCtorPattern = new OpCodeCtorPattern();
        var containerCtor = containerType.Methods.First(m => m.Name == ".ctor");
        if (!containerCtor.HasMethodBody || containerCtor.CilMethodBody!.Instructions.Count < vmOpCodes.Count * containerCtorPattern.Pattern.Count)
            throw new Exception("VM OpCode container .ctor is invalid or too small");

        var containerCtorOpCodes = PatternMatcher.GetAllMatchingInstructions(containerCtorPattern, containerCtor);
        foreach (var opCodeFieldInstrs in containerCtorOpCodes)
        {
            var opCode = opCodeFieldInstrs[1].GetLdcI4Constant();
            var operandType = opCodeFieldInstrs[2].GetLdcI4Constant();
            
            var matchingVMOpCodes = vmOpCodes.Where(x => x.SerializedInstructionField == opCodeFieldInstrs[4].Operand).ToList();
            if (matchingVMOpCodes.Count <= 0 && Ctx.Options.VeryVerbose)
            {
                Ctx.Console.InfoStr("Unknown OpCode", $"{opCode}, {operandType}");
                continue;
            }
            
            foreach (var vmOpCode in matchingVMOpCodes)
            {
                vmOpCode.HasVirtualCode = true;
                vmOpCode.VirtualCode = opCode;
                vmOpCode.VirtualOperandType = operandType;
            }
        }
        
        return true;
    }

    private MethodDefinition FindOpCodeMethod()
    {
        foreach (var method in Ctx.VMDeclaringType.Methods)
        {
            if (!method.IsPrivate || !method.IsStatic || method.Parameters.Count != 1 || !method.Signature!.ReturnsValue ||
                !method.Signature.ReturnType.FullName.StartsWith("System.Collections.Generic.Dictionary"))
                continue;

            return method;
        }
        
        throw new Exception("Unable to find dictionary method");
    }
    
    public OpCodeMapping(DevirtualizationContext ctx) : base(ctx)
    {
    }
}