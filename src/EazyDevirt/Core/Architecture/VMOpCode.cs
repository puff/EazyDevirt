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
                6 => CilOperandType.InlineI, // 5
                0 => CilOperandType.ShortInlineI, // 6
                10 => CilOperandType.InlineI8, // 8
                8 => CilOperandType.InlineR, // 12
                4 => CilOperandType.ShortInlineR, //3 
                5 => CilOperandType.InlineVar,        // 2      // used for both locals and arguments
                3 => CilOperandType.ShortInlineVar,   // 4      // used for both locals and arguments
                2 => CilOperandType.InlineTok,  //0
                9 => CilOperandType.InlineSwitch, //1
                1 => CilOperandType.InlineBrTarget, //10       // in eazfuscator, this is unsigned
                12 => CilOperandType.InlineArgument, //11        // this doesn't seem to be used, might not be correct 
                7 => CilOperandType.ShortInlineArgument,//7    // this doesn't seem to be used, might not be correct
                11 => CilOperandType.InlineNone, // 9

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