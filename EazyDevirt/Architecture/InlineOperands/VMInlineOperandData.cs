namespace EazyDevirt.Architecture.InlineOperands;

/// <summary>
/// Operand data.
/// </summary>
internal abstract record VMInlineOperandData(VMInlineOperandType Type)
{
    /// <summary>
    /// Describes the type of operand data.
    /// </summary>
    public VMInlineOperandType Type { get; } = Type;

    /// <summary>
    /// Read some inline operand data from a BinaryReader.
    /// </summary>
    /// <param name="reader">BinaryReader</param>
    /// <returns>InlineOperandData</returns>
    public static VMInlineOperandData Read(BinaryReader reader)
    {
        var operandType = reader.ReadByte();
        return (VMInlineOperandType)operandType switch
        {
            VMInlineOperandType.Type => new VMTypeData(reader),
            // VMInlineOperandType.Field => new VMFieldData(reader),
            // VMInlineOperandType.Method => new VMMethodData(reader),
            // VMInlineOperandType.UserString => new StringData(reader),
            // VMInlineOperandType.UnknownType => new VMUnknownType(reader),
            _ => null!
            // _ => throw new ArgumentOutOfRangeException(nameof(operandType), "Not a valid inline operand type!")
        };
    }
}

/// <summary>
/// Type-related operand data.
/// </summary>
internal record VMTypeData : VMInlineOperandData
{
    public string Name { get; }
    public bool HasGenericTypes { get; }
    public bool IsGenericParameterType { get; }
    public int GenericArgumentIndex { get; } 
    public int DeclaringTypeGenericArgumentIndex { get; }
    public VMInlineOperand[] GenericTypes { get; }
    
    // public string TypeNameWithoutNamespace => TypeName.Contains('.') ? TypeName.Split('.').Last() : string.Empty;
    //
    // public string Namespace => TypeName.Contains('.') ? string.Join(".", TypeName.Split('.').Reverse().Skip(1).Reverse().ToArray()) : TypeName;

    public string TypeName => Name.Contains(", ") ? Name.Split(',')[0] : Name;

    // public string AssemblyFullName => Name.Substring(TypeName.Length + 2, Name.Length - (TypeName.Length + 2));
    //
    // public string AssemblyName => AssemblyFullName.Split(',')[0];

    public VMTypeData(BinaryReader reader) : base(VMInlineOperandType.Type)
    {
        Name = reader.ReadString();
        HasGenericTypes = reader.ReadBoolean();
        IsGenericParameterType = reader.ReadBoolean();
        GenericArgumentIndex = reader.ReadInt32();
        DeclaringTypeGenericArgumentIndex = reader.ReadInt32();
        GenericTypes = VMInlineOperand.ReadArrayInternal(reader);
    }
}