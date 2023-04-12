using EazyDevirt.Devirtualization;

namespace EazyDevirt.Core.Abstractions;

internal abstract class StageBase
{
    protected StageBase(Context ctx)
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
    private protected Context Ctx { get; }

    /// <summary>
    /// Initializes the stage.
    /// </summary>
    /// <returns>Whether initialization was successful.</returns>
    private protected virtual bool Init() => true;

    /// <summary>
    /// Executes the stage.
    /// </summary>
    public abstract bool Run();

}