using EazyDevirt.Logging;

namespace EazyDevirt.Core;

public record Options
{
    /// <summary>
    ///     Target assembly info
    /// </summary>
    public FileInfo Assembly { get; init; }

    /// <summary>
    ///     Path of output directory
    /// </summary>
    public DirectoryInfo OutputPath { get; init; }

    /// <summary>
    ///     Verbosity level
    /// </summary>
    public VerboseLevel Verbosity { get; init; }

    /// <summary>
    ///     Shows useful debug information
    /// </summary>
    public bool Verbose
    {
        get { return Verbosity >= VerboseLevel.None; }
    }

    /// <summary>
    ///     Shows more verbose information
    /// </summary>
    public bool VeryVerbose
    {
        get { return Verbosity > VerboseLevel.Verbose; }
    }

    /// <summary>
    ///     Shows even more verbose information
    /// </summary>
    public bool VeryVeryVerbose
    {
        get { return Verbosity > VerboseLevel.VeryVeryVerbose; }
    }

    /// <summary>
    ///     Preserves all metadata tokens
    /// </summary>
    public bool PreserveAll { get; init; }

    /// <summary>
    ///     Keeps all obfuscator types
    /// </summary>
    public bool KeepTypes { get; init; }

    /// <summary>
    ///     Save output even if devirtualization fails
    /// </summary>
    public bool SaveAnyway { get; init; }

    /// <summary>
    ///     Only save successfully devirtualized methods
    /// </summary>
    /// <remarks>
    ///     This only matters if you're using the Save Anyway option
    /// </remarks>
    public bool OnlySaveDevirted { get; init; }
}