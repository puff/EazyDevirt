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

 /// TODO: update Verbose and VeryVerbose description
 /// <summary>
 /// Shows useful debug information
 /// </summary>
 public bool Verbose => Verbosity >= 1;

 /// <summary>
 /// Shows verbose information and more useful debugging information
 /// </summary>
 public bool VeryVerbose => Verbosity > 1;
 
 /// <summary>
 /// Preserves all metadata tokens
 /// </summary>
 public bool PreserveAll { get; init; }
 
 /// <summary>
 /// Keeps all obfuscator types
 /// </summary>
 public bool KeepTypes { get; init; }
}