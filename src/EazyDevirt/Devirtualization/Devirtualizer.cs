using EazyDevirt.Core.Abstractions;
using EazyDevirt.Devirtualization.Pipeline;

namespace EazyDevirt.Devirtualization;

internal class Devirtualizer
{
    public Devirtualizer(DevirtualizationContext ctx)
    {
        Ctx = ctx;
        Pipeline = new List<StageBase>
        {
            new ResourceParsing(ctx),
            new OpCodeMapping(ctx),
            new MethodDiscovery(ctx),
            new MethodDevirtualizer(ctx),
        };
    }
    
    private DevirtualizationContext Ctx { get; }
    private List<StageBase> Pipeline { get; }

    public bool Run()
    {
        foreach (var stage in Pipeline)
        {
            Ctx.Console.Info($"Executing {stage.Name}...");
            if (!stage.Run())
            {
                Ctx.Console.Error($"Failed executing {stage.Name}!");
                return false;
            }
            
            Ctx.Console.Success($"Executed {stage.Name}!");
        }

        return true;
    }
}