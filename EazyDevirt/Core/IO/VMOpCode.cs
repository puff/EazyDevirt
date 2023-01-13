using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Architecture;

namespace EazyDevirt.Core.IO;

internal record VMOpCode(SerializedFieldDefinition SerializedInstructionField, SerializedMethodDefinition SerializedDelegateMethod)
{
    /// <summary>
    /// Instruction field. These are all initialized in the .ctor of the container.
    /// </summary>
    public SerializedFieldDefinition SerializedInstructionField { get; } = SerializedInstructionField;
    
    /// <summary>
    /// The delegate method associated with this virtual instruction in the dictionary method.
    /// </summary>
    public SerializedMethodDefinition SerializedDelegateMethod { get; } = SerializedDelegateMethod;
    
    /// <summary>
    /// The virtual opcode, set when the instruction field is constructed.
    /// </summary>
    public int VirtualCode { get; set; } 
    
    /// <summary>
    /// The virtual operand type, set when the instruction field is constructed.
    /// </summary>
    public int VirtualOperandType { get; set; }
    
    /// <summary>
    /// Whether or not the virtual opcode was successfully extracted from the container .ctor method.
    /// </summary>
    public bool HasVirtualCode { get; set; }

    /// <summary>
    /// Associated CIL opcode.
    /// </summary>
    public CilOpCode CilOpCode { get; set; }
    
    /// <summary>
    /// Associated special opcode.
    /// </summary>
    public SpecialOpCode SpecialOpCode { get; set; }
    
    /// <summary>
    /// Whether or not the virtual instruction was identified with a legitimate CIL or special opcode.
    /// </summary>
    public bool IsIdentified { get; set; }
    
    /// <summary>
    /// Whether or not the associated opcode is a special opcode.
    /// </summary>
    public bool IsSpecial { get; set; }

    public override string ToString() =>
        $"VirtualCode: {VirtualCode} | VirtualOperandType: {VirtualOperandType} | " +
        $"CilOpCode: {CilOpCode} | SpecialOpCode {SpecialOpCode} |" +
        $"SerializedInstructionField: {SerializedInstructionField.MetadataToken} | SerializedDelegateMethod: {SerializedDelegateMethod.MetadataToken}";
}