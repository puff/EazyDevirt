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
    }

    private static void Run(DevirtualizationOptions options)
    {
        var ctx = new Context(options);
        Context.Instance = ctx;
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

        // TODO: Implement this in code or remove this option
        var keepTypesOption = new Option<bool>(new[] { "--keep-types", "-kt"}, "Keeps obfuscator types");
        keepTypesOption.SetDefaultValue(false);
        
        var saveAnywayOption = new Option<bool>(new[] { "--save-anyway"}, "Saves output of devirtualizer even if it fails");
        saveAnywayOption.SetDefaultValue(false);
        
        var onlySaveDevirtedOption = new Option<bool>(new[] { "--only-save-devirted"}, "Only saves successfully devirtualized methods (This option only matters if you use the save anyway option)");
        onlySaveDevirtedOption.SetDefaultValue(false);

        var rootCommand = new RootCommand("EazyDevirt is a tool to automatically restore the original IL code " +
                                          "from an assembly virtualized with Eazfuscator.NET")
        {
            inputArgument,
            outputArgument,
            verbosityOption,
            preserveAllOption,
            keepTypesOption,
            saveAnywayOption,
            onlySaveDevirtedOption
        };
        
        rootCommand.SetHandler(Run, 
            new DevirtualizationOptionsBinder(inputArgument, outputArgument, verbosityOption,
                preserveAllOption, keepTypesOption, saveAnywayOption, onlySaveDevirtedOption));
        
        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelp()
            .Build();
    }
}