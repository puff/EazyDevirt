using AsmResolver.DotNet;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Architecture;

internal record VMMethod(MethodDefinition Parent, string EncodedMethodKey)
{
    public MethodDefinition Parent { get; } = Parent;
    public string EncodedMethodKey { get; } = EncodedMethodKey;
    public long MethodKey { get; set; }
    public VMMethodInfo MethodInfo { get; set; }
    public List<VMExceptionHandler> VMExceptionHandlers { get; set; }
    
    public override string ToString() =>
        $"Parent: 0x{Parent.MetadataToken.ToInt32():X} | EncodedMethodKey: {EncodedMethodKey} | MethodKey: 0x{MethodKey:X} | " +
        $"MethodInfo: [{MethodInfo}] | VMExceptionHandlers: [{string.Join(", ", VMExceptionHandlers)}]";
}

// TODO: The order of these fields are scrambled across samples. See issue #3
internal record VMMethodInfo
{
    public int DeclaringType { get; }
    public string Name { get; }
    public byte BindingFlags { get; }
    public bool DeclaredOnly => (BindingFlags & 2) > 0;
    public bool IsInstance => (BindingFlags & 4) > 0;
    public bool IsStatic => (BindingFlags & 8) > 0;
    public int ReturnType { get; }
    public List<VMLocal> VMLocals { get; }
    public List<VMParameter> VMParameters { get; }

    public VMMethodInfo(BinaryReader reader)
    {
        DeclaringType = reader.ReadInt32();
        Name = reader.ReadString();
        BindingFlags = reader.ReadByte();
        ReturnType = reader.ReadInt32();

        VMLocals = new List<VMLocal>((int)reader.ReadInt16());
        for (var i = 0; i < VMLocals.Capacity; i++)
            VMLocals.Add(new VMLocal(reader.ReadInt32()));
        
        VMParameters = new List<VMParameter>(reader.ReadInt16());
        for (var i = 0; i < VMParameters.Capacity; i++)
            VMParameters.Add(new VMParameter(reader.ReadInt32(), reader.ReadBoolean()));
    }

    public override string ToString() =>
        $"DeclaringType: 0x{DeclaringType:X} | Name: {Name} | BindingFlags: {BindingFlags} | " +
        $"DeclaredOnly: {DeclaredOnly} | IsInstance: {IsInstance} | IsStatic: {IsStatic} | " +
        $"ReturnType: 0x{ReturnType:X} | VMLocals: [{string.Join(", ", VMLocals)}] | VMParameters: [{string.Join(", ", VMParameters)}]";
}

internal record VMLocal(int VMType)
{
    public int VMType { get; } = VMType;
    
    public override string ToString() => $"VMType: 0x{VMType:X}";
}

internal record VMParameter(int VMType, bool In)
{
    public int VMType { get; } = VMType;
    public bool In { get; } = In;
    
    public override string ToString() => $"VMType: 0x{VMType:X} | In: {In}";
}