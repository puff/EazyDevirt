using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using AsmResolver.DotNet.Builder;
using EazyDevirt.Devirtualization;
using EazyDevirt.Devirtualization.Options;

namespace EazyDevirt;

internal static class Program
{
    private static async Task Main(params string[] args)
    {
        Console.WriteLine(Logo);
        var parser = BuildParser();

        await parser.InvokeAsync(args).ConfigureAwait(false);
    }

    private static void Run(DevirtualizationOptions options)
    {
        var ctx = new DevirtualizationContext(options);
        var devirtualizer = new Devirtualizer(ctx);
        
        devirtualizer.Run();

        ctx.Options.OutputPath.Create();
        var outputFilePath = ctx.Options.OutputPath.FullName + '\\' + Path.GetFileNameWithoutExtension(ctx.Options.Assembly.Name) +
                             "-devirt" + ctx.Options.Assembly.Extension;
        ctx.Module.Write(outputFilePath,
            new ManagedPEImageBuilder(
                new DotNetDirectoryFactory(
                    ctx.Options.PreserveAll ? MetadataBuilderFlags.PreserveAll : MetadataBuilderFlags.PreserveMethodDefinitionIndices)));
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
        
        var verbosityOption = new Option<int>(new[] { "--verbose", "-v" }, "Level of verbosity output");
        verbosityOption.SetDefaultValue(0);

        var preserveAllOption = new Option<bool>(new[] { "--preserve-all", "-p"}, "Preserves all metadata tokens");
        preserveAllOption.SetDefaultValue(false);
        
        var keepTypesOption = new Option<bool>(new[] { "--keep-types", "-kt"}, "Keeps obfuscator types");
        keepTypesOption.SetDefaultValue(false);
        
        var rootCommand = new RootCommand("EazyDevirt is a tool to automatically restore the original IL code " +
                                          "from an assembly virtualized with Eazfuscator.NET")
        {
            inputArgument,
            outputArgument,
            verbosityOption,
            preserveAllOption,
            keepTypesOption,
        };
        
        rootCommand.SetHandler(Run, 
            new DevirtualizationOptionsBinder(inputArgument, outputArgument, verbosityOption,
                preserveAllOption, keepTypesOption));
        
        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelp()
            .Build();
    }

    private static string Logo =>
        "▄███▄   ██   ▄▄▄▄▄▄ ▀▄    ▄ \n" +
        "█▀   ▀  █ █ ▀   ▄▄▀   █  █  \n" +
        "██▄▄    █▄▄█ ▄▀▀   ▄▀  ▀█   \n" +
        "█▄   ▄▀ █  █ ▀▀▀▀▀▀    █    \n" +
        "▀███▀      █         ▄▀     \n" +
        "          █                 \n" +
        "         ▀                  ";
}