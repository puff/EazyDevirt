using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using EazyDevirt.Abstractions;
using EazyDevirt.Core.IO;
using EazyDevirt.PatternMatching;
using EazyDevirt.PatternMatching.Patterns;
using EazyDevirt.PatternMatching.Patterns.OpCodes;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace EazyDevirt.Devirtualization.Pipeline;

internal class OpCodeMapping : Stage
{
    private protected override bool Init()
    {
        var executeVMMethodPattern = new ExecuteVMMethodPattern();
        var executeVMMethod =
            Ctx.VMDeclaringType.Methods.FirstOrDefault(method =>
                PatternMatcher.GetAllMatchingInstructions(executeVMMethodPattern, method).Count >= 1);
        
        if (executeVMMethod == null)
        {
            Ctx.Console.Error("Failed to find ExecuteVMMethod method!");
            return false;
        }

        Ctx.VMExecuteVMMethod = executeVMMethod;

        var argumentFieldMatches =
            PatternMatcher.GetAllMatchingInstructions(new ArgumentFieldPattern(), executeVMMethod);
        if (argumentFieldMatches.Count <= 0)
        {
            Ctx.Console.Error("Failed to find VM Arguments field!");
            return false;
        }

        if (argumentFieldMatches.First()[1].Operand is not SerializedFieldDefinition argumentField)
        {
            Ctx.Console.Error("VM Arguments field is not correct!");
            return false;
        }

        Ctx.VMArgumentsField = argumentField;

        var localField = Ctx.VMDeclaringType.Fields.FirstOrDefault(field =>
            field != argumentField &&
            field.Signature!.FieldType.FullName == argumentField.Signature!.FieldType.FullName);
        
        if (localField == null)
        {
            Ctx.Console.Error("Failed to find VM Locals field!");
            return false;
        }
        
        Ctx.VMLocalsField = localField;
        return true;
    }

    public override bool Run()
    {
        if (!Init()) return false;

        var dictMethod = FindOpCodeMethod();
        if (dictMethod == null)
        {
            Ctx.Console.Error("Unable to find vm opcode dictionary method.");
            return false;
        }

        if (Ctx.Options.VeryVerbose)
            Ctx.Console.InfoStr("VM opcode dictionary method", dictMethod.MetadataToken);

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
            Ctx.Console.InfoStr("VM opcodes with handlers", vmOpCodes.Count);

        if (containerType == null)
        {
            Ctx.Console.Error("VM opcode container type is null.");
            return false;
        }

        var containerCtorPattern = new OpCodeCtorPattern();
        var containerCtor = containerType.Methods.First(m => m.Name == ".ctor");
        if (!containerCtor.HasMethodBody || containerCtor.CilMethodBody!.Instructions.Count <
            vmOpCodes.Count * containerCtorPattern.Pattern.Count)
        {
            Ctx.Console.Error("VM opcode container .ctor is invalid or too small");
            return false;
        }

        var containerCtorOpCodes = PatternMatcher.GetAllMatchingInstructions(containerCtorPattern, containerCtor);
        if (Ctx.Options.VeryVerbose)
            Ctx.Console.InfoStr("Total VM opcodes found", containerCtorOpCodes.Count);
        
        foreach (var opCodeFieldInstrs in containerCtorOpCodes)
        {
            var opCode = opCodeFieldInstrs[1].GetLdcI4Constant();
            var operandType = opCodeFieldInstrs[2].GetLdcI4Constant();
            
            var matchingVMOpCodes = vmOpCodes.Where(x => x.SerializedInstructionField == opCodeFieldInstrs[4].Operand).ToList();
            if (matchingVMOpCodes.Count <= 0 && Ctx.Options.VeryVerbose)
            {
                Ctx.Console.InfoStr("Unhandled VM opcode", $"{opCode}, {operandType}");
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

        var identified = 0f;
        foreach (var vmOpCode in vmOpCodes)
        {
            if (!vmOpCode.HasVirtualCode)
            {
                Ctx.Console.Warning($"VM opcode [{vmOpCode}] does not have a virtual opcode!");
                continue;
            }

            var opCodePat = Ctx.PatternMatcher.FindOpCode(vmOpCode);
            if (opCodePat != null)
            {
                vmOpCode.IsIdentified = true;
                vmOpCode.CilOpCode = opCodePat.CilOpCode;
                vmOpCode.SpecialOpCode = opCodePat.SpecialOpCode;
                vmOpCode.IsSpecial = opCodePat.IsSpecial;
            }
            else if (Ctx.Options.VeryVeryVerbose)
                Ctx.Console.Warning($"Failed to identify VM opcode [{vmOpCode}]");

            Ctx.PatternMatcher.SetOpCodeValue(vmOpCode.VirtualCode, vmOpCode);

            if (!vmOpCode.IsIdentified) continue;
            identified++;
            if (Ctx.Options.VeryVeryVerbose)
                Ctx.Console.Info(vmOpCode);
        }
        
        if (Ctx.Options.VeryVerbose)
            Ctx.Console.InfoStr($"VM opcodes identified ({identified / vmOpCodes.Count:P0})", identified);
                
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