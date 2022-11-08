namespace EazyDevirt.Devirtualization.Options;

public record DevirtualizationOptions
{
 /// <summary>
 /// Target assembly info
 /// </summary>
#pragma warning disable CS8618
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
 /// TODO: update this and VeryVerbose description
 /// Shows useful debug information
 /// </summary>
 public bool Verbose => Verbosity == 1;

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