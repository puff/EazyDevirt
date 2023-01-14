using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;
using EazyDevirt.Devirtualization.Options;
using EazyDevirt.Logging;
using EazyDevirt.PatternMatching;

namespace EazyDevirt.Devirtualization;

internal record DevirtualizationContext
{
    public DevirtualizationOptions Options { get; }
    public ModuleDefinition Module { get; }
    public PatternMatcher PatternMatcher { get; }
    public ConsoleLogger Console { get; }
    
    public MetadataToken VMResourceGetterMdToken { get; set; }
    public TypeDefinition VMDeclaringType { get; set; }
    public VMCipherStream VMStream { get; set; }
    public VMCipherStream VMResolverStream { get; set; }
    public int PositionCryptoKey { get; set; }
    public int MethodCryptoKey { get; set; }
    
    public List<VMMethod> VMMethods { get; set; }
    
    public DevirtualizationContext(DevirtualizationOptions opts)
    {
        Options = opts;
        Module = ModuleDefinition.FromFile(Options.Assembly.FullName);
        PatternMatcher = new PatternMatcher();
        Console = new ConsoleLogger();
    }
}