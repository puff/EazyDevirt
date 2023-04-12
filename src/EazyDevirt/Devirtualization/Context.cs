using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Core.IO;
using EazyDevirt.Devirtualization.Options;
using EazyDevirt.Logging;
using EazyDevirt.PatternMatching;

namespace EazyDevirt.Devirtualization;

internal record Context
{
    public static Context Instance { get; set; } = null!;
    
    public DevirtualizationOptions Options { get; }
    public ModuleDefinition Module { get; }
    public static PatternMatcher PatternMatcher
    {
        get { return PatternMatcher.GetInstance(); }
    }

    public ConsoleLogger Console { get; } = new();
    
    public ReferenceImporter Importer { get; }
    public MetadataToken VMResourceGetterMdToken { get; set; }
    
    /// <summary>
    /// VM Type fields set in the vm type's constructor.
    /// Used in pattern matching opcodes.
    /// </summary>
    public Dictionary<FieldDefinition, ITypeDefOrRef> VMTypeFields { get; set; }

    /// <summary>
    /// VM method locals field.
    /// Used in pattern matching ldloc and stloc opcodes.
    /// </summary>
    public FieldDefinition VMLocalsField { get; set; }

    /// <summary>
    /// VM method arguments field.
    /// Used in pattern matching ldarg and starg opcodes.
    /// </summary>
    public FieldDefinition VMArgumentsField { get; set; }
    public MethodDefinition VMExecuteVMMethod { get; set; }
    public TypeDefinition VMDeclaringType { get; set; }
    public VMCipherStream VMStream { get; set; }
    public VMCipherStream VMResolverStream { get; set; }
    public int PositionCryptoKey { get; set; }
    public int MethodCryptoKey { get; set; }

    public List<VMMethod> VMMethods { get; set; }

    public Context(DevirtualizationOptions opts)
    {
        Options = opts;
        Module = ModuleDefinition.FromFile(Options.Assembly.FullName);

        Importer = Module.DefaultImporter;
        Importer.ImportScope(new AssemblyReference(Module.Assembly));
    }
}