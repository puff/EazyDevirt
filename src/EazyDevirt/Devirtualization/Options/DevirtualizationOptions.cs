namespace EazyDevirt.Devirtualization.Options;

internal record DevirtualizationOptions
{
#pragma warning disable CS8618
 /// <summary>
 /// Target assembly info
 /// </summary>
 public FileInfo Assembly { get; init; }

 /// <summary>
 /// Path of output directory
 /// </summary>
 public DirectoryInfo OutputPath { get; init; }
#pragma warning restore CS8618

 /// <summary>
 /// Verbosity level
 /// </summary>
 public int Verbosity { get; init; }

 /// <summary>
 /// Shows useful debug information
 /// </summary>
 public bool Verbose => Verbosity >= 1;

 /// <summary>
 /// Shows more verbose information
 /// </summary>
 public bool VeryVerbose => Verbosity > 1;
 
 /// <summary>
 /// Shows even more verbose information
 /// </summary>
 public bool VeryVeryVerbose => Verbosity > 2;
 
 /// <summary>
 /// Preserves all metadata tokens
 /// </summary>
 public bool PreserveAll { get; init; }
 
 /// <summary>
 /// Don't verify labels or compute max stack for devirtualized methods
 /// </summary>
 public bool NoVerify { get; init; }
 
 /// <summary>
 /// Keeps all obfuscator types
 /// </summary>
 public bool KeepTypes { get; init; }
 
 /// <summary>
 /// Save output even if devirtualization fails
 /// </summary>
 public bool SaveAnyway { get; init; }
 
 /// <summary>
 /// Only save successfully devirtualized methods
 /// </summary>
 /// <remarks>
 /// This only matters if you're using the Save Anyway option
 /// </remarks>
 public bool OnlySaveDevirted { get; init; }
 
 /// <summary>
 /// Require dependencies when resolving generic methods
 /// </summary>
 /// <remarks>
 /// If this is disabled, methods utilizing generics (type or method args) may not have proper signatures if dependencies aren't able to be resolved
 /// </remarks>
 public bool RequireDepsForGenericMethods { get; init; }
}