namespace EazyDevirt.Architecture.InlineOperands;

// thank you to saneki

/// <summary>
/// A guess as to the first byte (enum) of VMInlineOperand.
/// </summary>
internal enum ValueType
{
    /// <summary>
    /// The Value field holds a raw MetadataToken value.
    /// </summary>
    Token = 0,

    /// <summary>
    /// The Value field holds a position.
    /// </summary>
    Position = 1
}

/// <summary>
/// Inline operand types.
/// </summary>
/// <remarks>
/// TODO: These change across samples.
///       These indexes can be found in the vmethod_0 override for each operand type's struct.
/// </remarks>
internal enum VMInlineOperandType
{
    Type = 0,
    Field = 1,
    Method = 2,
    EazCall = 3,
    UserString = 4
}

/// <summary>
/// Deserialized inline operand.
/// </summary>
internal record VMInlineOperand
{
    /// <summary>
    /// Determines how the Value field is interpreted.
    /// </summary>
    public ValueType ValueType { get; }

    /// <summary>
    /// Either a raw metadata token from the parent module, or a position in
    /// the VM resource stream.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Deserialized data associated with this operand.
    /// </summary>
    public VMInlineOperandData Data { get; }
    
    /// <summary>
    /// Whether or not this operand contains a token.
    /// </summary>
    public bool IsToken => ValueType == ValueType.Token;

    /// <summary>
    /// Whether or not this operand contains a position.
    /// </summary>
    public bool IsPosition => !IsToken;
    
    /// <summary>
    /// Get the operand's token, throwing an exception if none.
    /// </summary>
    public int Token => IsToken ? Value : throw new Exception("InlineOperand has no token (only position)");

    /// <summary>
    /// Get the operand's position, throwing an exception if none.
    /// </summary>
    public int Position => IsPosition ? Value : throw new Exception("InlineOperand has no position (only token)");

    /// <summary>
    /// Whether or not this operand has deserialized data associated with it.
    /// </summary>
    public bool HasData => Data != null;

    public VMInlineOperand(ValueType valueType, int value)
    {
        ValueType = valueType;
        Value = value;
    }
    
    public VMInlineOperand(BinaryReader reader)
    {
        ValueType = (ValueType)reader.ReadByte();

        if (ValueType == ValueType.Token)
            Value = reader.ReadInt32();
        else
            Data = VMInlineOperandData.Read(reader);
    }

    public static VMInlineOperand ReadInternal(BinaryReader reader) => new(ValueType.Position, reader.ReadInt32());

    public static VMInlineOperand[] ReadArrayInternal(BinaryReader reader)
    {
        var count = reader.Read7BitEncodedInt();
        var arr = new VMInlineOperand[count];

        for (var i = 0; i < arr.Length; i++)
            arr[i] = ReadInternal(reader);

        return arr;
    }
}