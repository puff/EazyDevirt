using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Core.IO;
using EazyDevirt.Logging;
using EazyDevirt.PatternMatching;

namespace EazyDevirt.Core;

internal record Context
{
    public Context(Options options)
    {
        Options = options;

        Console = new ConsoleLogger(options.Verbosity);
        Module = ModuleDefinition.FromFile(Options.Assembly.FullName);

        Importer = Module.DefaultImporter;
        Importer.ImportScope(new AssemblyReference(Module.Assembly!));
    }

    public static Context Instance { get; set; } = null!;

    public Options Options { get; }

    public ModuleDefinition Module { get; }

    public static PatternMatcher PatternMatcher
    {
        get { return PatternMatcher.GetInstance(); }
    }

    public ConsoleLogger Console { get; }

    public ReferenceImporter Importer { get; }
    public MetadataToken VMResourceGetterMdToken { get; set; }

    /// <summary>
    ///     VM Type fields set in the vm type's constructor.
    ///     Used in pattern matching opcodes.
    /// </summary>
    public Dictionary<FieldDefinition, ITypeDefOrRef> VMTypeFields { get; set; } = null!;

    /// <summary>
    ///     VM method locals field.
    ///     Used in pattern matching ldloc and stloc opcodes.
    /// </summary>
    public FieldDefinition VMLocalsField { get; set; } = null!;

    /// <summary>
    ///     VM method arguments field.
    ///     Used in pattern matching ldarg and starg opcodes.
    /// </summary>
    public FieldDefinition VMArgumentsField { get; set; } = null!;

    public TypeDefinition VMDeclaringType { get; set; } = null!;
    public VMCipherStream VMStream { get; set; } = null!;
    public VMCipherStream VMResolverStream { get; set; } = null!;

    public int PositionCryptoKey { get; set; }
    public int MethodCryptoKey { get; set; }

    public List<VMMethod> VMMethods { get; set; } = null!;
}