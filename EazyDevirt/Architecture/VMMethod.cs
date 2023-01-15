using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures.Types;

namespace EazyDevirt.Architecture;

internal record VMMethod(MethodDefinition Parent, string EncodedMethodKey)
{
    public MethodDefinition Parent { get; } = Parent;
    public string EncodedMethodKey { get; } = EncodedMethodKey;
    public long MethodKey { get; set; }
    public VMMethodInfo MethodInfo { get; set; }
    public List<VMExceptionHandler> VMExceptionHandlers { get; set; }
    
    public CilInstructionCollection Instructions { get; set; }
    public List<CilExceptionHandler> ExceptionHandlers { get; set; }
    
    public override string ToString() =>
        $"Parent: {Parent.MetadataToken} | EncodedMethodKey: {EncodedMethodKey} | MethodKey: 0x{MethodKey:X} | " +
        $"MethodInfo: [{MethodInfo}] | VMExceptionHandlers: [{string.Join(", ", VMExceptionHandlers)}] | " +
        $"Instructions: {Instructions?.Count}";
}

// TODO: The order of these fields are scrambled across samples. See issue #3
internal record VMMethodInfo
{
    public int VMDeclaringType { get; }
    public string Name { get; }
    public byte BindingFlags { get; }
    public bool DeclaredOnly => (BindingFlags & 2) > 0;
    public bool IsInstance => (BindingFlags & 4) > 0;
    public bool IsStatic => (BindingFlags & 8) > 0;
    public int VMReturnType { get; }
    public List<VMLocal> VMLocals { get; }
    public List<VMParameter> VMParameters { get; }

    public TypeSignature DeclaringType { get; set; }
    public TypeSignature ReturnType { get; set; }
    
    public VMMethodInfo(BinaryReader reader)
    {
        VMDeclaringType = reader.ReadInt32();
        Name = reader.ReadString();
        BindingFlags = reader.ReadByte();
        VMReturnType = reader.ReadInt32();

        VMLocals = new List<VMLocal>((int)reader.ReadInt16());
        for (var i = 0; i < VMLocals.Capacity; i++)
            VMLocals.Add(new VMLocal(reader.ReadInt32()));
        
        VMParameters = new List<VMParameter>(reader.ReadInt16());
        for (var i = 0; i < VMParameters.Capacity; i++)
            VMParameters.Add(new VMParameter(reader.ReadInt32(), reader.ReadBoolean()));
    }

    public override string ToString() =>
        $"VMDeclaringType: 0x{VMDeclaringType:X} | Name: {Name} | BindingFlags: {BindingFlags} | " +
        $"DeclaredOnly: {DeclaredOnly} | IsInstance: {IsInstance} | IsStatic: {IsStatic} | " +
        $"VMReturnType: 0x{VMReturnType:X} | VMLocals: [{string.Join(", ", VMLocals)}] | VMParameters: [{string.Join(", ", VMParameters)}] | " +
        $"DeclaringType: {DeclaringType.FullName} | ReturnType: {ReturnType.FullName}";
}

internal record VMLocal(int VMType)
{
    public int VMType { get; } = VMType;
    
    // public TypeSignature Type { get; set; }
    
    public override string ToString() => $"VMType: 0x{VMType:X}"; // | Type: {Type.FullName}
}

internal record VMParameter(int VMType, bool In)
{
    public int VMType { get; } = VMType;
    public bool In { get; } = In;
    
    // public TypeSignature Type { get; set; }

    public override string ToString() => $"VMType: 0x{VMType:X} | In: {In}"; // | Type: {Type.FullName}
}