using System.Reflection;

namespace EazyDevirt.Architecture.InlineOperands;

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
            VMInlineOperandType.UserString => new VMStringData(reader),
            VMInlineOperandType.UnknownType => new VMUnknownTypeData(reader),
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

/// <summary>
/// Field operand data.
/// </summary>
internal record VMFieldData : VMInlineOperandData
{
    public VMInlineOperand FieldType { get; }
    public string Name { get; }
    public bool Flags { get; }
    
    public BindingFlags BindingFlags
    {
        get
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
            bindingFlags |= Flags ? BindingFlags.Static : BindingFlags.Instance;
            return bindingFlags;
        }
    }
    
    public VMFieldData(BinaryReader reader) : base(VMInlineOperandType.Field)
    {
        FieldType = VMInlineOperand.ReadInternal(reader);
        Name = reader.ReadString();
        Flags = reader.ReadBoolean();
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
    public bool IsInstance => !IsStatic;
    public string Name { get; } 
    public VMInlineOperand ReturnType { get; } 
    public VMInlineOperand[] Parameters { get; } 
    public VMInlineOperand[] GenericArguments { get; } 

    public bool HasGenericArguments => GenericArguments.Length > 0;
    
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
        IsStatic = (Flags & 1) != 0;
        Name = reader.ReadString();
        ReturnType = VMInlineOperand.ReadInternal(reader);
        Parameters = VMInlineOperand.ReadArrayInternal(reader);
        GenericArguments = VMInlineOperand.ReadArrayInternal(reader);
    }
}

/// <summary>
/// String operand data.
/// </summary>
internal record VMStringData : VMInlineOperandData
{
    /// <summary>
    /// String value.
    /// </summary>
    public string Value { get; }

    public VMStringData(BinaryReader reader) : base(VMInlineOperandType.UserString)
    {
        Value = reader.ReadString();
    }
}

internal record VMUnknownTypeData : VMInlineOperandData
{
    public int Unknown1 { get; }
    public int Unknown2 { get; }

    public VMUnknownTypeData(BinaryReader reader) : base(VMInlineOperandType.UnknownType)
    {
        Unknown1 = reader.ReadInt32();
        Unknown2 = reader.ReadInt32();
    }
}