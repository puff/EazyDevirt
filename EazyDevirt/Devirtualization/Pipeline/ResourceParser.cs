using EazyDevirt.Abstractions;

namespace EazyDevirt.Devirtualization.Pipeline;

public class ResourceParser : IStage
{
    public string Name => nameof(ResourceParser);

    public void Run(DevirtualizationContext ctx)
    {
        // the fun begins...
    }
}