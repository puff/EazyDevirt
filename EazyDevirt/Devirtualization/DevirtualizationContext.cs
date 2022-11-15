using System.Security.Cryptography;
using AsmResolver.DotNet;
using EazyDevirt.Devirtualization.Options;
using EazyDevirt.Logging;
using EazyDevirt.PatternMatching;

namespace EazyDevirt.Devirtualization;

internal record DevirtualizationContext
{
    public DevirtualizationContext(DevirtualizationOptions opts)
    {
        Options = opts;
        Module = ModuleDefinition.FromFile(Options!.Assembly.FullName);
        PatternMatcher = new PatternMatcher();
        Console = new ConsoleLogger();
    }
    
    public DevirtualizationOptions Options { get; }
    public ModuleDefinition Module { get; }
    public PatternMatcher PatternMatcher { get; }
    public ConsoleLogger Console { get; }
}