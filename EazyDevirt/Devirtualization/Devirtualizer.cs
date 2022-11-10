using EazyDevirt.Abstractions;
using EazyDevirt.Devirtualization.Options;
using EazyDevirt.Devirtualization.Pipeline;

namespace EazyDevirt.Devirtualization;

public class Devirtualizer
{
    public Devirtualizer(DevirtualizationContext ctx)
    {
        Context = ctx;
        Pipeline = new List<IStage>
        {
            new ResourceParser(),
        };
    }
    
    private DevirtualizationContext Context { get; }
    private List<IStage> Pipeline { get; }

    public void Run()
    {
        foreach (var stage in Pipeline)
        {
            Context.Console.Info($"Executing {stage.Name}...");
            stage.Run(Context);
            Context.Console.Success($"Executed {stage.Name}!");
        }
    }
}