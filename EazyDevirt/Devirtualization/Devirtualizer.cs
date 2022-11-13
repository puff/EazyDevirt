using EazyDevirt.Abstractions;
using EazyDevirt.Devirtualization.Pipeline;

namespace EazyDevirt.Devirtualization;

public class Devirtualizer
{
    public Devirtualizer(DevirtualizationContext ctx)
    {
        Context = ctx;
        Pipeline = new List<Stage>
        {
            new ResourceParser(ctx),
        };
    }
    
    private DevirtualizationContext Context { get; }
    private List<Stage> Pipeline { get; }

    public void Run()
    {
        foreach (var stage in Pipeline)
        {
            Context.Console.Info($"Executing {stage.Name}...");
            if (!stage.Run())
                Context.Console.Error($"Failed executing {stage.Name}!");
            else
                Context.Console.Success($"Executed {stage.Name}!");
        }
    }
}