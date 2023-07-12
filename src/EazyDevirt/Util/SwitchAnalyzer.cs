using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using Echo.Platforms.AsmResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EazyDevirt.Util
{
    internal class SwitchAnalyzer
    {
        public MethodDefinition CurrentMethod { get; private set; }
        public CilInstruction SwitchInstruction { get; private set; }

        bool _read;
        List<CilInstruction[]> caseCilInstructions { get; }
        public int CaseCount => caseCilInstructions.Count;


        public SwitchAnalyzer(MethodDefinition currentMethod, CilInstruction switchInstruction)
        {
            if (switchInstruction.OpCode != CilOpCodes.Switch)
                throw new Exception("Invalid instruction! Instruction must have a switch opcode!");

            if (switchInstruction.Operand == null)
                throw new Exception("Invalid instruction! Switch instruction must have an operand!");

            CurrentMethod = currentMethod;
            SwitchInstruction = switchInstruction;
            caseCilInstructions = new List<CilInstruction[]>();
            _read = false;
        }

        public bool Read()
        {
            if (_read)
                return false;

            var graph = this.CurrentMethod?.CilMethodBody?.ConstructStaticFlowGraph();
            var switchNode = graph?.Nodes.FirstOrDefault(node => node?.Contents.Footer == this.SwitchInstruction, null);

            //it is impossible for the function to not have a switch opcode
            if (switchNode == null)
                return false;

            foreach (var adjNode in switchNode.ConditionalEdges)
                caseCilInstructions.Add(adjNode.Target.Contents.Instructions.ToArray());

            return true;
        }

        public CilInstruction[] GetCase(int caseIndex)
            => caseCilInstructions[caseIndex];
    }
}
