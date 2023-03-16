using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;

namespace EazyDevirt.Core.Architecture;

internal record VMOpCode(SerializedFieldDefinition SerializedInstructionField = null!, SerializedMethodDefinition SerializedDelegateMethod = null!)
{
    public static  VMOpCode DefaultNopOpCode { get; } = new();
    
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
    /// The virtual operand type integer, set when the instruction field is constructed.
    /// </summary>
    public int VirtualOperandType { get; set; }
    
    /// <summary>
    /// The CIL operand type.
    /// </summary>
    // TODO: Implement pattern matching for operand types
    public CilOperandType CilOperandType
    {
        get
        {
            return VirtualOperandType switch
            {
                1 => CilOperandType.InlineI,
                4 => CilOperandType.ShortInlineI,
                0 => CilOperandType.InlineI8,
                9 => CilOperandType.InlineR,
                12 => CilOperandType.ShortInlineR,
                5 => CilOperandType.InlineVar,              // used for both locals and arguments
                3 => CilOperandType.ShortInlineVar,         // used for both locals and arguments
                2 => CilOperandType.InlineTok,
                7 => CilOperandType.InlineSwitch,
                6 => CilOperandType.InlineBrTarget,        // in eazfuscator, this is unsigned
                11 => CilOperandType.InlineArgument,        // this doesn't seem to be used, might not be correct 
                8 => CilOperandType.ShortInlineArgument,    // this doesn't seem to be used, might not be correct
                10 => CilOperandType.InlineNone,

                _ => throw new ArgumentOutOfRangeException(nameof(VirtualOperandType), VirtualOperandType, "Unknown operand type")
            };
        }
    }
    
    /// <summary>
    /// Whether or not the virtual opcode was successfully extracted from the container .ctor method.
    /// </summary>
    public bool HasVirtualCode { get; set; }

    /// <summary>
    /// Associated CIL opcode.
    /// </summary>
    public CilOpCode? CilOpCode { get; set; } = CilOpCodes.Nop;
    
    /// <summary>
    /// Associated special opcode.
    /// </summary>
    public SpecialOpCodes? SpecialOpCode { get; set; }
    
    /// <summary>
    /// Whether or not the virtual instruction was identified with a legitimate CIL or special opcode.
    /// </summary>
    public bool IsIdentified { get; set; }
    
    /// <summary>
    /// Whether or not the associated opcode is a special opcode.
    /// </summary>
    public bool IsSpecial { get; set; }

    public override string ToString() =>
        $"VirtualCode: {VirtualCode} | OperandType: {CilOperandType} ({VirtualOperandType}) | " +
        $"CilOpCode: {CilOpCode} | SpecialOpCode: {SpecialOpCode} | " +
        $"SerializedInstructionField: {SerializedInstructionField?.MetadataToken} | SerializedDelegateMethod: {SerializedDelegateMethod?.MetadataToken}";
}