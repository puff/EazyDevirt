using AsmResolver.DotNet;
using EazyDevirt.Devirtualization.Options;
using EazyDevirt.Logging;

namespace EazyDevirt.Devirtualization;

public record DevirtualizationContext
{
    public DevirtualizationContext(DevirtualizationOptions opts)
    {
        Options = opts;
        Module = ModuleDefinition.FromFile(Options.Assembly.FullName);
        Console = new ConsoleLogger();
    }
    
    public DevirtualizationOptions Options { get; }
    public ModuleDefinition Module { get; }
    public ConsoleLogger Console { get; }
}