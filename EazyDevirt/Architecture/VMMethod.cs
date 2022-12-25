using AsmResolver.DotNet;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Architecture;

internal record VMMethod(MethodDefinition Parent, string EncodedMethodKey)
{
    public MethodDefinition Parent { get; } = Parent;
    public string EncodedMethodKey { get; } = EncodedMethodKey;
    public long MethodKey { get; set; }
    public VMMethodInfo MethodInfo { get; set; }
}

internal record VMMethodInfo
{
    public int DeclaringType { get; }
    public string Name { get; }
    public byte BindingFlags { get; }
    public bool DeclaredOnly => (BindingFlags & 2) > 0;
    public bool IsInstance => (BindingFlags & 4) > 0;
    public bool IsStatic => (BindingFlags & 8) > 0;
    public int ReturnType { get; }
    private List<VMLocal> VMLocals { get; }
    private List<VMParameter> VMParameters { get; }

    public VMMethodInfo(VMBinaryReader reader)
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

    public override string ToString()
    {
        return $"DeclaringType: {DeclaringType} | Name: {Name} | BindingFlags: {BindingFlags} | " +
               $"DeclaredOnly: {DeclaredOnly} | IsInstance: {IsInstance} | IsStatic {IsStatic} | " +
               $"ReturnType: {ReturnType} | VMLocals Count: {VMLocals.Count} | VM Parameters Count: {VMParameters.Count}";
    }
}

internal record VMLocal(int Type)
{
    public int Type { get; } = Type;
}

internal record VMParameter(int Type, bool In)
{
    public int Type { get; } = Type;
    public bool In { get; } = In;
}