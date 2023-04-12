using EazyDevirt.Logging;

namespace EazyDevirt.Core.Abstractions;

internal abstract class StageBase
{
    protected StageBase(Context ctx)
    {
        Name = GetType().Name;
        Ctx = ctx;
    }

    /// <summary>
    ///     Name of stage.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Devirtualization Context.
    /// </summary>
    private protected Context Ctx { get; }

    protected ConsoleLogger Logger
    {
        get { return Ctx.Console; }
    }

    /// <summary>
    ///     Initializes the stage.
    /// </summary>
    /// <returns>Whether initialization was successful.</returns>
    private protected virtual bool Init()
    {
        return true;
    }

    /// <summary>
    ///     Executes the stage.
    /// </summary>
    public abstract bool Run();
}