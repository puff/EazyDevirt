using EazyDevirt.Abstractions;
using EazyDevirt.Devirtualization.Pipeline;

namespace EazyDevirt.Devirtualization;

internal class Devirtualizer
{
    public Devirtualizer(DevirtualizationContext ctx)
    {
        Context = ctx;
        Pipeline = new List<Stage>
        {
            new ResourceParsing(ctx),
            // TODO: add binaryreader and field order matching stage
            new OpCodeMapping(ctx),
            new MethodDiscovery(ctx),
            new MethodDisassembler(ctx),
        };
    }
    
    private DevirtualizationContext Context { get; }
    private List<Stage> Pipeline { get; }

    public bool Run()
    {
        foreach (var stage in Pipeline)
        {
            Context.Console.Info($"Executing {stage.Name}...");
            if (!stage.Run())
            {
                Context.Console.Error($"Failed executing {stage.Name}!");
                return false;
            }
            else
                Context.Console.Success($"Executed {stage.Name}!");
        }

        return true;
    }
}