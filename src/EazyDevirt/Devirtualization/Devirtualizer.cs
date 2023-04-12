using EazyDevirt.Core;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Devirtualization.Pipeline;
using EazyDevirt.Logging;

namespace EazyDevirt.Devirtualization;

internal class Devirtualizer
{
    public Devirtualizer(Context ctx)
    {
        Ctx = ctx;
        Pipeline = new List<StageBase>
        {
            new ResourceParsing(ctx),
            // TODO: add binaryreader and field order matching stage
            new OpCodeMapping(ctx),
            new MethodDiscovery(ctx),
            new MethodDevirtualizer(ctx)
            // TODO: add data devirtualizer
        };
    }

    private Context Ctx { get; }
    private List<StageBase> Pipeline { get; }

    public bool Run()
    {
        var logger = Ctx.Console;

        foreach (var stage in Pipeline)
        {
            logger.Info($"Executing {stage.Name}...", VerboseLevel.None);
            
            if (!stage.Run())
            {
                logger.Error($"Failed executing {stage.Name}!");
                return false;
            }

            logger.Success($"Executed {stage.Name}!", VerboseLevel.None);
        }

        return true;
    }
}