using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
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

        Console.ReadLine();
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

        var hmPasswordsOption = new Option<string[]>(new[] {"--hm-pass"}, "Homomorphic password(s) keyed by mdtoken, supporting multiple passwords per method with optional 1-based ordering. Formats: mdtoken:order:type:value | mdtoken:type:value. Types: sbyte, byte, short, ushort, int, uint, long, ulong, string. String uses UTF-16. Repeatable; passwords are consumed in the specified order per method.")
        {
            Arity = ArgumentArity.ZeroOrMore
        };
        
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
