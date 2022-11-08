using System.CommandLine;
using System.CommandLine.Binding;

namespace EazyDevirt.Devirtualization.Options;

internal class DevirtualizationOptionsBinder : BinderBase<DevirtualizationOptions>
{
    private readonly Argument<FileInfo> _assemblyOption;
    private readonly Option<DirectoryInfo> _outputPathOption;
    private readonly Option<int> _verbosityOption;
    private readonly Option<bool> _preserveAllOption;
    private readonly Option<bool> _keepTypesOption;

    public DevirtualizationOptionsBinder(Argument<FileInfo> assemblyOption, Option<DirectoryInfo> outputPathOption, 
        Option<int> verbosityOption, Option<bool> preserveAllOption, Option<bool> keepTypesOption)
    {
        _assemblyOption = assemblyOption;
        _outputPathOption = outputPathOption;
        _verbosityOption = verbosityOption;
        _preserveAllOption = preserveAllOption;
        _keepTypesOption = keepTypesOption;
    }

    protected override DevirtualizationOptions GetBoundValue(BindingContext bindingContext) =>
        new DevirtualizationOptions
        {
            Assembly = bindingContext.ParseResult.GetValueForArgument(_assemblyOption),
            OutputPath = bindingContext.ParseResult.GetValueForOption(_outputPathOption)!,
            Verbosity = bindingContext.ParseResult.GetValueForOption(_verbosityOption),
            PreserveAll = bindingContext.ParseResult.GetValueForOption(_preserveAllOption),
            KeepTypes = bindingContext.ParseResult.GetValueForOption(_keepTypesOption),
        };
}