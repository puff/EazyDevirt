using AsmResolver.DotNet.Serialized;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.PatternMatching.Patterns;
using EazyDevirt.PatternMatching;
using AsmResolver.DotNet;
using EazyDevirt.Core.Architecture;
using AsmResolver.PE.DotNet.Cil;

using ValueType = EazyDevirt.Core.Architecture.InlineOperands.ValueType;

namespace EazyDevirt.Devirtualization
{
    internal class ReadOrderAnalyzer : StageBase
    {
        public ReadOrderAnalyzer(DevirtualizationContext ctx) : base(ctx)
        {
        }

        //Set field function order is the same accross different versions
        VMMethodField[] FieldOrder =
        {
            VMMethodField.Locals,
            VMMethodField.Parameters,
            VMMethodField.Name,
            VMMethodField.ReturnType,
            VMMethodField.VMDeclaringType,
            VMMethodField.BindingFlags
        };

        public override bool Run()
        {
            //find type resolver order
            this.Ctx.OperandReadOrder = AnalyzeTypeResolverOrder(this.Ctx);
            if (this.Ctx.OperandReadOrder.Count == 0)
            {
                Ctx.Console.Error($"Failed to find Correct Reading order for Operand Reader!");
                return false;
            }

            Ctx.Console.Success("Found Correct Operand Read Order!");

            //Analyze VM Data Read Order
            MethodDefinition? vmFuncReader = FindMethodReadOrderFunction(this.Ctx);
            if (vmFuncReader == null)
            {
                Ctx.Console.Error($"Failed to find VM Method Reader");
                return false;
            }

            var readOrder = AnalyzeVMDataReadOrder(vmFuncReader);

            if (readOrder.Count != FieldOrder.Length)
            {
                Ctx.Console.Error($"Failed to analyze VMMethod Read Order {vmFuncReader?.MetadataToken}");
                return false;
            }

            string orderStrFormat = ": " + string.Join(", ", readOrder.Select((d, i) => string.Format("[{0}] {1}", i + 1, d)));

            if (Ctx.Options.VeryVerbose)
                Ctx.Console.InfoStr(orderStrFormat, "Correct VMMethod Read Order");
            Ctx.Console.Success("Found Correct Method Read Order!");

            this.Ctx.VMMethodReadOrder = readOrder;
            return true;
        }

        private List<VMMethodField> AnalyzeVMDataReadOrder(MethodDefinition readFunc)
        {
            List<VMMethodField> readOrder = new();
            var funcRetType = readFunc?.Signature?.ReturnType.Resolve();

            //find all set function inside VMMethod Type (order is same across different versions)
            var methodsToAnalyze = funcRetType?.Methods
                .Where(m => m.Parameters.Count == 1)
                .OrderBy(z => z.MetadataToken.ToInt32())
                .Zip(FieldOrder, (fieldType, readerSetFunc) => (readerSetFunc, fieldType))
                .ToDictionary(x => x.fieldType, x => x.readerSetFunc);

            var instructions = readFunc?.CilMethodBody?.Instructions;
            int i, j;
            for (i = 0, j = 0; j < methodsToAnalyze?.Count; i++)
            {
                if (instructions?[i].OpCode == CilOpCodes.Callvirt && instructions[i].Operand is SerializedMethodDefinition smd
                    && methodsToAnalyze.ContainsKey(smd))
                {
                    readOrder.Add(methodsToAnalyze[smd]);
                    j++;

                }
            }

            return readOrder;
        }

        private List<ValueType> AnalyzeTypeResolverOrder(DevirtualizationContext ctx)
        {
            List<ValueType> order = new();
            foreach (var t in ctx.Module.GetAllTypes())
            {
                foreach (var method in t.Methods)
                {
                    if (!method.HasMethodBody || method?.CilMethodBody == null || method?.CilMethodBody?.Instructions.Count == 0 || method?.Parameters.Count != 1)
                        continue;

                    var matchedInstrs = PatternMatcher.GetAllMatchingInstructions(new OperandResolverPattern(), method);
                    if (matchedInstrs.Count > 0)
                    {
                        var lastIndex = method.CilMethodBody.Instructions.GetIndexByOffset(matchedInstrs.First().Last().Offset);
                        var currInstr = method.CilMethodBody.Instructions[lastIndex + 1];
                        var secInstr = method.CilMethodBody.Instructions[lastIndex + 2];

                        if (currInstr.IsLdcI4() && secInstr.IsConditionalBranch()) //2021 sample
                        {
                            if (currInstr.GetLdcI4Constant() == 1)
                            {
                                order.Add(ValueType.Position);
                                order.Add(ValueType.Token);
                                return order;
                            }
                            else
                            {
                                //from older sample
                                order.Add(ValueType.Position);
                                order.Add(ValueType.Token);
                                return order;
                            }
                        }
                        else
                        {
                            order.Add(ValueType.Token);
                            order.Add(ValueType.Position);
                            return order;
                        }
                    }

                }
            }

            return order;
        }

        private MethodDefinition? FindMethodReadOrderFunction(DevirtualizationContext ctx)
        {
            foreach (var t in ctx.Module.GetAllTypes())
            {
                foreach (var method in t.Methods)
                {
                    if (!method.HasMethodBody || method?.CilMethodBody?.Instructions.Count == 0 || method?.Parameters.Count != 3)
                        continue;

                    if (method.Parameters[0].ParameterType.FullName == "System.IO.Stream"
                        && method.Parameters[1].ParameterType.FullName == "System.Int64"
                        && method.Parameters[2].ParameterType.FullName == "System.String")
                    {
                        var matchedInstrs = PatternMatcher.GetAllMatchingInstructions(new ReadVMMethodPattern(), method);
                        if (matchedInstrs.Count > 0)
                        {
                            var ReadVMFunc = matchedInstrs.First()[4].Operand as SerializedMethodDefinition;
                            return ReadVMFunc;
                        }
                    }

                }
            }

            return null;
        }
    }
}
