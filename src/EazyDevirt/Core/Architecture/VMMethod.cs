using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace EazyDevirt.Core.Architecture;

internal record VMMethod(MethodDefinition Parent, string EncodedMethodKey, long MethodKey)
{
    public MethodDefinition Parent { get; } = Parent;
    public string EncodedMethodKey { get; } = EncodedMethodKey;
    public long MethodKey { get; } = MethodKey;
    public VMMethodInfo MethodInfo { get; set; }
    public List<VMExceptionHandler> VMExceptionHandlers { get; set; }
    
    public List<CilExceptionHandler> ExceptionHandlers { get; set; }
    // public List<Parameter> Parameters { get; set; }
    public List<CilLocalVariable> Locals { get; set; }
    public List<CilInstruction> Instructions { get; set; }
    
    public bool SuccessfullyDevirtualized { get; set; }
    public bool HasHomomorphicEncryption { get; set; }
    public int CodeSize { get; set; }
    public int CurrentVirtualOffset { get; set; }
    /// <summary>
    /// For each instruction index, stores the VM "virtual offset" captured at decode-time.
    /// </summary>
    public List<uint> InstructionVirtualOffsets { get; set; }
    /// <summary>
    /// Mapping from VM virtual offset to CIL offset, built once during decoding.
    /// </summary>
    public Dictionary<uint, int> VmToCilOffsetMap { get; set; }
    public long InitialCodeStreamPosition { get; set; }
    /// <summary>
    /// Stack holding Homomorphic Encryption ending positions. Used to calculate virtual <-> CIL offsets.
    /// </summary>
    public Stack<int> HMEndPositionStack { get; set; }
    
    public override string ToString() =>
        $"Parent: {Parent.MetadataToken} | EncodedMethodKey: {EncodedMethodKey} | MethodKey: 0x{MethodKey:X} | " +
        $"MethodInfo: [{MethodInfo}] | VMExceptionHandlers: [{string.Join(", ", VMExceptionHandlers)}] | " +
        $"ExceptionHandlers: {ExceptionHandlers?.Count} | Locals: {Locals?.Count} | " +
        $"Instructions: {Instructions?.Count}";
}

// TODO: The order of these fields are scrambled across samples. See issue #3
internal record VMMethodInfo
{
    public int VMDeclaringType { get; }
    public string Name { get; }
    public byte BindingFlags { get; }
    public bool IsStatic => (BindingFlags & 2) > 0;
    public bool IsSecurityCritical => (BindingFlags & 4) > 0;
    public bool IsUnknownFlag2 => (BindingFlags & 8) > 0;
    public int VMReturnType { get; }
    public List<VMLocal> VMLocals { get; }
    public List<VMParameter> VMParameters { get; }

    public ITypeDefOrRef DeclaringType { get; set; }
    public ITypeDefOrRef ReturnType { get; set; }
    
    public VMMethodInfo(BinaryReader reader)
    {
        VMDeclaringType = reader.ReadInt32();
        Name = reader.ReadString();
        BindingFlags = reader.ReadByte();
        VMReturnType = reader.ReadInt32();

        VMLocals = new List<VMLocal>(reader.ReadInt16());
        for (var i = 0; i < VMLocals.Capacity; i++)
            VMLocals.Add(new VMLocal(reader.ReadInt32()));
        
        VMParameters = new List<VMParameter>(reader.ReadInt16());
        for (var i = 0; i < VMParameters.Capacity; i++)
            VMParameters.Add(new VMParameter(reader.ReadInt32(), reader.ReadBoolean()));
    }

    public override string ToString() =>
        $"VMDeclaringType: 0x{VMDeclaringType:X} | Name: {Name} | BindingFlags: {BindingFlags} | " +
        $"IsStatic: {IsStatic} | IsSecurityCritical: {IsSecurityCritical} | IsUnknownFlag2: {IsUnknownFlag2} | " +
        $"VMReturnType: 0x{VMReturnType:X} | VMLocals: [{string.Join(", ", VMLocals)}] | VMParameters: [{string.Join(", ", VMParameters)}] | " +
        $"DeclaringType: {DeclaringType.FullName} | ReturnType: {ReturnType.FullName}";
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