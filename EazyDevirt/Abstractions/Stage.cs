using EazyDevirt.Devirtualization;

namespace EazyDevirt.Abstractions;

internal abstract class Stage
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
    
    /// <summary>
    /// Devirtualization Context.
    /// </summary>
    private protected DevirtualizationContext Ctx { get; }

    /// <summary>
    /// Initializes the stage.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns>Whether initialization was successful.</returns>
    private protected virtual bool Init() => true;

    /// <summary>
    /// Executes the stage.
    /// </summary>
    public abstract bool Run();

}