using System.Reflection;

namespace EazyDevirt.Core.Architecture.InlineOperands;

// thank you to saneki

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
            VMInlineOperandType.Field => new VMFieldData(reader),
            VMInlineOperandType.Method => new VMMethodData(reader),
            VMInlineOperandType.UserString => new VMUserStringData(reader),
            VMInlineOperandType.EazCall => new VMEazCallData(reader),
            _ => throw new ArgumentOutOfRangeException(nameof(operandType), "Not a valid inline operand type!")
        };
    }
}

/// <summary>
/// Type operand data.
/// </summary>
internal record VMTypeData : VMInlineOperandData
{
    public string Name { get; }
    public bool HasGenericTypeArgs { get; }
    public bool IsGenericParameterType { get; }
    public int GenericArgumentIndex { get; } 
    public int GenericTypeArgumentIndex { get; }
    public VMInlineOperand[] GenericTypes { get; }

    public TypeName TypeName { get; }
    
    public VMTypeData(BinaryReader reader) : base(VMInlineOperandType.Type)
    {
        Name = reader.ReadString();
        HasGenericTypeArgs = reader.ReadBoolean();
        IsGenericParameterType = reader.ReadBoolean();
        GenericArgumentIndex = reader.ReadInt32();
        GenericTypeArgumentIndex = reader.ReadInt32();
        GenericTypes = VMInlineOperand.ReadArrayInternal(reader);

        TypeName = new TypeName(Name);
    }
}

/// <summary>
/// Field operand data.
/// </summary>
internal record VMFieldData : VMInlineOperandData
{
    public VMInlineOperand DeclaringType { get; }
    public string Name { get; }
    public bool IsStatic { get; }
    
    public BindingFlags BindingFlags
    {
        get
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
            bindingFlags |= IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            return bindingFlags;
        }
    }
    
    public VMFieldData(BinaryReader reader) : base(VMInlineOperandType.Field)
    {
        DeclaringType = VMInlineOperand.ReadInternal(reader);
        Name = reader.ReadString();
        IsStatic = reader.ReadBoolean();
    }
}

/// <summary>
/// Method operand data.
/// </summary>
internal record VMMethodData : VMInlineOperandData
{
    public VMInlineOperand DeclaringType { get; }
    public byte Flags { get; } 
    
    public bool IsStatic { get; } 
    public bool HasGenericArguments { get; }
    public string Name { get; } 
    public VMInlineOperand ReturnType { get; } 
    public VMInlineOperand[] Parameters { get; } 
    public VMInlineOperand[] GenericArguments { get; } 
    
    public BindingFlags BindingFlags
    {
        get
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
            bindingFlags |= IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            return bindingFlags;
        }
    }

    public VMMethodData(BinaryReader reader) : base(VMInlineOperandType.Method)
    {
        DeclaringType = VMInlineOperand.ReadInternal(reader);
        Flags = reader.ReadByte();
        IsStatic = (Flags & 2) > 0;
        HasGenericArguments = (Flags & 1) > 0;
        Name = reader.ReadString();
        ReturnType = VMInlineOperand.ReadInternal(reader);
        Parameters = VMInlineOperand.ReadArrayInternal(reader);
        GenericArguments = VMInlineOperand.ReadArrayInternal(reader);
    }
}

/// <summary>
/// String operand data.
/// </summary>
internal record VMUserStringData : VMInlineOperandData
{
    /// <summary>
    /// String value.
    /// </summary>
    public string Value { get; }

    public VMUserStringData(BinaryReader reader) : base(VMInlineOperandType.UserString)
    {
        Value = reader.ReadString();
    }
}

/// <summary>
/// Eaz call operand data.
/// </summary>
internal record VMEazCallData : VMInlineOperandData
{
    /// <summary>
    /// Contains the flags of the vm method contained.
    /// </summary>
    public int EazCallValue { get; }
    
    /// <summary>
    /// The method's operand data vm stream position.
    /// </summary>
    public int VMMethodPosition { get; }

    public VMEazCallData(BinaryReader reader) : base(VMInlineOperandType.EazCall)
    {
        EazCallValue = reader.ReadInt32();
        VMMethodPosition = reader.ReadInt32();
    }
}