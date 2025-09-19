using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using EazyDevirt.Devirtualization;
using EazyDevirt.Devirtualization.Options;

namespace EazyDevirt;

internal static class Program
{
    private static readonly Version CurrentVersion = new("1.0.0");
    private static readonly Version CurrentEazVersion = new("2022.2.763.35371");

    private static async Task Main(params string[] args)
    {
        var parser = BuildParser();

        await parser.InvokeAsync(args).ConfigureAwait(false);
    }

    private static void Run(DevirtualizationOptions options)
    {
        var ctx = new DevirtualizationContext(options);
        DevirtualizationContext.Instance = ctx;
        ctx.Console.ShowInfo(CurrentVersion, CurrentEazVersion);
        
        var devirtualizer = new Devirtualizer(ctx);
        if (!devirtualizer.Run())
        {
            ctx.Console.Error("Failed to devirtualize executable!");
            if (!options.SaveAnyway)
                return;
        }

        ctx.Options.OutputPath.Create();
        var outputFilePath = ctx.Options.OutputPath.FullName + '\\' + Path.GetFileNameWithoutExtension(ctx.Options.Assembly.Name) +
                             "-devirt" + ctx.Options.Assembly.Extension;

        var dnFactory =
            new DotNetDirectoryFactory(ctx.Options.PreserveAll
                ? MetadataBuilderFlags.PreserveAll
                : MetadataBuilderFlags.None)
            {
                MethodBodySerializer = new CilMethodBodySerializer
                {
                    ComputeMaxStackOnBuildOverride = false
                }
            };

        ctx.Module.Write(outputFilePath,
            new ManagedPEImageBuilder(
                dnFactory));
        ctx.Console.Success($"Saved file to {outputFilePath}");
    }
    
    private static Parser BuildParser()
    {
        var inputArgument = new Argument<FileInfo>("assembly", "Path to target assembly").ExistingOnly();
        inputArgument.AddValidator(result =>
        {
            var ext = result.GetValueForArgument(inputArgument).Extension;
            if (ext != ".exe" && ext != ".dll")
                result.ErrorMessage = $"Invalid file extension type '{ext}'. Valid extensions are '.exe' and '.dll'.";
        });
        
        var outputArgument = new Argument<DirectoryInfo>("output", "Path to output directory");
        outputArgument.SetDefaultValue(new DirectoryInfo("./eazydevirt-output"));
        
        var verbosityOption = new Option<int>(new[] { "--verbose", "-v" }, "Level of verbosity [1: Verbose, 2: Very Verbose, 3: Very Very Verbose]");
        verbosityOption.SetDefaultValue(0);
        verbosityOption.ArgumentHelpName = "verbosity";

        var preserveAllOption = new Option<bool>(new[] { "--preserve-all"}, "Preserves all metadata tokens");
        preserveAllOption.SetDefaultValue(false);

        var noVerifyOption = new Option<bool>(new[] { "--no-verify"}, "Don't verify labels or compute max stack for devirtualized methods");
        noVerifyOption.SetDefaultValue(false);
        
        // TODO: Implement this in code or remove this option
        var keepTypesOption = new Option<bool>(new[] { "--keep-types", "-kt"}, "Keeps obfuscator types");
        keepTypesOption.SetDefaultValue(false);
        
        var saveAnywayOption = new Option<bool>(new[] { "--save-anyway"}, "Saves output of devirtualizer even if it fails");
        saveAnywayOption.SetDefaultValue(false);
        
        var onlySaveDevirtedOption = new Option<bool>(new[] { "--only-save-devirted"}, "Only saves successfully devirtualized methods (This option only matters if you use the save anyway option)");
        onlySaveDevirtedOption.SetDefaultValue(false);

        var requireDepsForGenerics = new Option<bool>(new[] { "--require-deps-for-generics"}, "Require dependencies when resolving generic methods for accuracy");
        requireDepsForGenerics.SetDefaultValue(true);

        var hmPasswordsOption = new Option<string[]>(new[] {"--hm-pass"}, "Homomorphic password(s) keyed by mdtoken, supporting multiple passwords per method with optional 1-based ordering. Formats: mdtoken:order:type:value | mdtoken:type:value. Types: sbyte, byte, short, ushort, int, uint, long, ulong, string. String values must be wrapped in double quotes (\"...\") and may contain colons; escape double quotes and backslashes with a backslash. Strings use UTF-16. Repeatable; passwords are consumed in the specified order per method.")
        {
            Arity = ArgumentArity.ZeroOrMore
        };

        hmPasswordsOption.AddValidator(result =>
        {
            var entries = result.GetValueForOption(hmPasswordsOption) ?? Array.Empty<string>();
            var errors = new List<string>();
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    errors.Add("--hm-pass: empty specification provided");
                    continue;
                }

                var parts = entry.Split(':', 4, StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                {
                    errors.Add($"--hm-pass '{entry}': invalid format. Expected 'mdtoken:type:value' or 'mdtoken:order:type:value'.");
                    continue;
                }

                var tokenStr = parts[0];
                if (!TryParseMdTokenLocal(tokenStr, out _))
                {
                    errors.Add($"--hm-pass '{entry}': invalid mdtoken '{tokenStr}'. Use hex with or without 0x, e.g. 0x060000AB.");
                    continue;
                }

                // Determine whether parts[1] is order or type, and compute value span accordingly.
                string normType;
                int valueStartIndex;
                if (TryParseOrderLocal(parts[1], out _))
                {
                    // Expect at least 4 parts: mdtoken:order:type:value
                    if (parts.Length < 4)
                    {
                        errors.Add($"--hm-pass '{entry}': invalid format. Expected 'mdtoken:order:type:value'.");
                        continue;
                    }
                    if (!TryNormalizeTypeLocal(parts[2], out normType))
                    {
                        errors.Add($"--hm-pass '{entry}': unknown type '{parts[2]}'.");
                        continue;
                    }
                    valueStartIndex = 3;
                }
                else if (TryNormalizeTypeLocal(parts[1], out normType))
                {
                    // Typed-only: mdtoken:type:value
                    if (parts.Length < 3)
                    {
                        errors.Add($"--hm-pass '{entry}': invalid format. Expected 'mdtoken:type:value'.");
                        continue;
                    }
                    valueStartIndex = 2;
                }
                else
                {
                    errors.Add($"--hm-pass '{entry}': invalid format. Expected 'mdtoken:type:value' or 'mdtoken:order:type:value'.");
                    continue;
                }

                var valueJoined = string.Join(":", parts, valueStartIndex, parts.Length - valueStartIndex);
                if (!TryValidateValueForTypeLocal(normType, valueJoined))
                {
                    errors.Add($"--hm-pass '{entry}': value '{valueJoined}' is not valid for type '{normType}'.");
                    continue;
                }
            }

            if (errors.Count > 0)
                result.ErrorMessage =
                    "Invalid --hm-pass specification(s):\n" +
                    string.Join(Environment.NewLine, errors) +
                    "\nAccepted formats: mdtoken:order:type:value | mdtoken:type:value";

            static bool TryParseMdTokenLocal(string s, out uint token)
            {
                token = 0;
                if (string.IsNullOrWhiteSpace(s)) return false;
                s = s.Trim();
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    s = s[2..];
                return uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out token);
            }

            static bool TryParseOrderLocal(string s, out int order)
            {
                order = 0;
                if (string.IsNullOrWhiteSpace(s)) return false;
                if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var o)) return false;
                if (o <= 0) return false;
                order = o; return true;
            }

            static bool TryNormalizeTypeLocal(string s, out string norm)
            {
                norm = string.Empty;
                if (string.IsNullOrWhiteSpace(s)) return false;
                switch (s.Trim().ToLowerInvariant())
                {
                    case "sbyte": case "i8": norm = "sbyte"; return true;
                    case "byte": case "u8": norm = "byte"; return true;
                    case "short": case "int16": case "i16": norm = "int16"; return true;
                    case "ushort": case "uint16": case "u16": norm = "uint16"; return true;
                    case "int": case "int32": case "i32": norm = "int32"; return true;
                    case "uint": case "uint32": case "u32": norm = "uint32"; return true;
                    case "long": case "int64": case "i64": norm = "int64"; return true;
                    case "ulong": case "uint64": case "u64": norm = "uint64"; return true;
                    case "string": case "str": norm = "string"; return true;
                    default: return false;
                }
            }

            static bool TryValidateValueForTypeLocal(string type, string value)
            {
                if (type == "string")
                {
                    // Must be wrapped in double quotes so colons can be included safely.
                    if (string.IsNullOrEmpty(value) || value.Length < 2) return false;
                    if (!(value[0] == '"' && value[^1] == '"')) return false;
                    // Validate escapes inside string: only allow \" and \\
                    for (int i = 1; i < value.Length - 1; i++)
                    {
                        if (value[i] == '\\')
                        {
                            if (i + 1 >= value.Length - 1) return false; // trailing backslash
                            var n = value[i + 1];
                            if (n == '"' || n == '\\')
                            { i++; continue; }
                            return false; // disallow other escapes (e.g., \' or \n)
                        }
                    }
                    return true;
                }
                if (string.IsNullOrWhiteSpace(value)) return false;
                var s = value.Trim();
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // For hex we accept any size and rely on unchecked casts in binder
                    return ulong.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
                }
                return type switch
                {
                    "sbyte" => sbyte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "byte" => byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "int16" => short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "uint16" => ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "int32" => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "uint32" => uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "int64" => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "uint64" => ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    _ => false
                };
            }
        });
        
        var rootCommand = new RootCommand("is an open-source tool that automatically restores the original IL code " +
                                          "from an assembly virtualized with Eazfuscator.NET")
        {
            inputArgument,
            outputArgument,
            verbosityOption,
            preserveAllOption,
            noVerifyOption,
            keepTypesOption,
            saveAnywayOption,
            onlySaveDevirtedOption,
            requireDepsForGenerics,
            hmPasswordsOption
        };

        rootCommand.SetHandler(Run,
            new DevirtualizationOptionsBinder(inputArgument, outputArgument, verbosityOption,
                preserveAllOption, noVerifyOption, keepTypesOption, saveAnywayOption, onlySaveDevirtedOption,
                requireDepsForGenerics, hmPasswordsOption));
        
        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelp()
            .Build();
    }
}
