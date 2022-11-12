using EazyDevirt.Devirtualization;

namespace EazyDevirt.Abstractions;

public abstract class Stage
{
    protected Stage(DevirtualizationContext ctx)
    {
        Name = GetType().Name;
        Ctx = ctx;
    }

    /// <summary>
    /// Name of stage.
    /// </summary>
    public string Name { get; }
    private protected DevirtualizationContext Ctx { get; }
    
    /// <summary>
    /// Initializes the stage.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns>Whether initialization was successful.</returns>
    private protected abstract bool Init();

    /// <summary>
    /// Executes the stage.
    /// </summary>
    public abstract bool Run();

}