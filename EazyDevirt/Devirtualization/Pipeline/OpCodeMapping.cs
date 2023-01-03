using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;
using EazyDevirt.PatternMatching;
using EazyDevirt.PatternMatching.Patterns;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace EazyDevirt.Devirtualization.Pipeline;

internal class OpCodeMapping : Stage
{
    public override bool Run()
    {
        if (!Init()) return false;

        var dictMethod = FindOpCodeMethod();
        if (dictMethod == null)
        {
            Ctx.Console.Error("Unable to find dictionary method");
            return false;
        }

        if (Ctx.Options.VeryVerbose)
            Ctx.Console.InfoStr(dictMethod.MetadataToken, "VM OpCode dictionary method");

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
            
            // in EazSample-eazfix-cleaned-named, this is the 0x040001D2 and 0x040001C0 opcode structs. and is the Thread.MemoryBarrier() opcode
            if (instructionField != instructionField2)
                vmOpCodes.Add(new VMOpCode(instructionField2!, opCodeDelegate!));

            vmOpCodes.Add(new VMOpCode(instructionField!, opCodeDelegate!));
        }
        
        if (Ctx.Options.VeryVerbose)
            Ctx.Console.InfoStr(vmOpCodes.Count, "VM OpCodes with handlers");

        if (containerType == null)
        {
            Ctx.Console.Error("VM OpCode container type cannot be null");
            return false;
        }

        var containerCtorPattern = new OpCodeCtorPattern();
        var containerCtor = containerType.Methods.First(m => m.Name == ".ctor");
        if (!containerCtor.HasMethodBody || containerCtor.CilMethodBody!.Instructions.Count <
            vmOpCodes.Count * containerCtorPattern.Pattern.Count)
        {
            Ctx.Console.Error("VM OpCode container .ctor is invalid or too small");
            return false;
        }

        var containerCtorOpCodes = PatternMatcher.GetAllMatchingInstructions(containerCtorPattern, containerCtor);
        if (Ctx.Options.VeryVerbose)
            Ctx.Console.InfoStr(containerCtorOpCodes.Count, "VM OpCodes found");
        
        foreach (var opCodeFieldInstrs in containerCtorOpCodes)
        {
            var opCode = opCodeFieldInstrs[1].GetLdcI4Constant();
            var operandType = opCodeFieldInstrs[2].GetLdcI4Constant();
            
            var matchingVMOpCodes = vmOpCodes.Where(x => x.SerializedInstructionField == opCodeFieldInstrs[4].Operand).ToList();
            if (matchingVMOpCodes.Count <= 0 && Ctx.Options.VeryVerbose)
            {
                Ctx.Console.InfoStr("Unused VM OpCode", $"{opCode}, {operandType}");
                continue;
            }
            
            // don't worry, this changes the vmOpCodes list too :)
            foreach (var vmOpCode in matchingVMOpCodes)
            {
                vmOpCode.HasVirtualCode = true;
                vmOpCode.VirtualCode = opCode;
                vmOpCode.VirtualOperandType = operandType;
            }
        }
        
        foreach (var vmOpCode in vmOpCodes)
        {
            if (!vmOpCode.HasVirtualCode)
            {
                Ctx.Console.Warning($"VM OpCode [{vmOpCode}] does not have a virtual code!");
                continue;
            }

            Ctx.PatternMatcher.SetOpCodeValue(vmOpCode.VirtualCode, vmOpCode);

            if (Ctx.Options.VeryVeryVerbose)
                Ctx.Console.Info(vmOpCode);
        }
        
        return true;
    }

    private MethodDefinition FindOpCodeMethod() =>
        Ctx.VMDeclaringType.Methods.FirstOrDefault(method => method is { IsPrivate: true, IsStatic: true, Parameters.Count: 1 } && 
                                                             method.Signature!.ReturnsValue && 
                                                             method.Signature.ReturnType.FullName.StartsWith("System.Collections.Generic.Dictionary")
        )!;

    public OpCodeMapping(DevirtualizationContext ctx) : base(ctx)
    {
    }
}