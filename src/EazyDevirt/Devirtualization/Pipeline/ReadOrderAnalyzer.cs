using AsmResolver.DotNet.Serialized;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.PatternMatching.Patterns;
using EazyDevirt.PatternMatching;
using AsmResolver.DotNet;
using EazyDevirt.Core.Architecture;
using AsmResolver.PE.DotNet.Cil;

using ValueType = EazyDevirt.Core.Architecture.InlineOperands.ValueType;
using EazyDevirt.Util;
using EazyDevirt.Core.Architecture.InlineOperands;

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
                Ctx.Console.Error($"Failed to find Correct Reading order for Type Resolver Reader!");
                return false;
            }

            Ctx.Console.Success("Found Correct Type Resolver Read Order!");

            var opDataBaseTdef = FindOperandDataBase(this.Ctx);
            if (opDataBaseTdef == null)
            {
                Ctx.Console.Error($"Failed to find VM Operand Base");
                return false;
            }

            //get all types that inherits the OperandDataBase
            var inheritedTypes = opDataBaseTdef.GetAllInheriting(opDataBaseTdef);
            if (inheritedTypes.Count == 0)
            {
                Ctx.Console.Error($"Failed to get inherited types of {opDataBaseTdef.FullName}");
                return false;
            }

            Ctx.VMOperandTypeOrder = AnalyzeOperandTypes(inheritedTypes);
            Ctx.Console.Success("Found Correct Operand Type Order!");

            //Analyze VM Data Read Order
            MethodDefinition? vmFuncReader = FindMethodReadOrderFunction(this.Ctx);
            if (vmFuncReader == null)
            {
                Ctx.Console.Error($"Failed to find VM Method Reader");
                return false;
            }

            var vmDataReadOrder = AnalyzeVMDataReadOrder(vmFuncReader);

            if (vmDataReadOrder.Count != FieldOrder.Length)
            {
                Ctx.Console.Error($"Failed to analyze VMMethod Read Order {vmFuncReader?.MetadataToken}");
                return false;
            }

            string orderStrFormat = ": " + string.Join(", ", vmDataReadOrder.Select((d, i) => string.Format("[{0}] {1}", i + 1, d)));

            if (Ctx.Options.VeryVerbose)
                Ctx.Console.InfoStr(orderStrFormat, "Correct VMMethod Read Order");
            Ctx.Console.Success("Found Correct Method Read Order!");

            this.Ctx.VMMethodReadOrder = vmDataReadOrder;
            return true;
        }

        private Dictionary<int, VMInlineOperandType> AnalyzeOperandTypes(List<TypeDefinition> opTypes)
        {
            Dictionary<int, VMInlineOperandType> opTypesOrder = new();

            //constants
            string Int32Type = "System.Int32";
            string BooleanType = "System.Boolean";
            string ByteType = "System.Byte";
            string StringType = "System.String";

            foreach(var opType in opTypes)
            {
                int? typeConstant = GetVMOperandTypeCode(opType);
                if (typeConstant == null)
                    throw new Exception("Operand Type Code should not be null!");

                if (opType.Fields.Count == 1 && Utils.GetFieldCountFromRetType(opType, StringType) == 1)
                    opTypesOrder[typeConstant.Value] = VMInlineOperandType.UserString;
                else if (opType.Fields.Count == 2 && Utils.GetFieldCountFromRetType(opType, Int32Type) == 2)
                    opTypesOrder[typeConstant.Value] = VMInlineOperandType.EazCall;
                else if (opType.Fields.Count == 3 && Utils.GetFieldCountFromRetType(opType, StringType) == 1 && Utils.GetFieldCountFromRetType(opType, BooleanType) == 1)
                    opTypesOrder[typeConstant.Value] = VMInlineOperandType.Field;
                else if (opType.Fields.Count == 6)
                {
                    if (Utils.GetFieldCountFromRetType(opType, StringType) == 1 && Utils.GetFieldCountFromRetType(opType, ByteType) == 1)
                        opTypesOrder[typeConstant.Value] = VMInlineOperandType.Method;
                    else if (Utils.GetFieldCountFromRetType(opType, StringType) == 1 && Utils.GetFieldCountFromRetType(opType, BooleanType) == 2 && Utils.GetFieldCountFromRetType(opType, Int32Type) == 3)
                        opTypesOrder[typeConstant.Value] = VMInlineOperandType.Type;
                }
            }

            return opTypesOrder;
        }

        private int? GetVMOperandTypeCode(TypeDefinition t)
        {
            foreach(var m in t.Methods)
            {
                if (!m.IsPublic || !m.IsVirtual || !m.IsSpecialName)
                    continue;

                if (!(m.Signature != null && m.CilMethodBody != null && m.Signature.ReturnsValue && m.Signature.ReturnType.ToString() == "System.Byte"))
                    continue;

                return m.CilMethodBody.Instructions[0].GetLdcI4Constant();
            }

            return null;
        }

        private TypeDefinition? FindOperandDataBase(DevirtualizationContext ctx)
        {
            foreach (var t in ctx.Module.GetAllTypes())
            {
                /* internal abstract class OperandBase
                    {
	                    protected OperandBase()
	                    {
	                    }

                        //Operand Code value corresponds to the switch value
	                    public abstract byte GetOperandCode();
                    }
                */
                if (!t.IsAbstract || !t.IsNotPublic || t.Methods.Count != 2)
                    continue;

                //there's only one method in the type that's not a constructor (GetOperandCode)
                MethodDefinition getOperandCodeFunc = t.Methods.First(g => !g.IsConstructor);

                //Need to confirm if the type is actually OperandDataBase
                if (getOperandCodeFunc.IsAbstract &&
                    getOperandCodeFunc.Signature is not null && getOperandCodeFunc.Signature.ReturnsValue &&
                    getOperandCodeFunc.Signature.ReturnType.ToString() == "System.Byte")
                    return t;
            }

            return null;
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

        private Dictionary<int, ValueType> AnalyzeTypeResolverOrder(DevirtualizationContext ctx)
        {
            Dictionary<int, ValueType> order = new();
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
                                order[0] = ValueType.Position;
                                order[1] = ValueType.Token;
                                return order;
                            }
                            else
                            {
                                //from older sample
                                order[0] = ValueType.Position;
                                order[1] = ValueType.Token;
                                return order;
                            }
                        }
                        else
                        {
                            order[0] = ValueType.Token;
                            order[1] = ValueType.Position;
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
