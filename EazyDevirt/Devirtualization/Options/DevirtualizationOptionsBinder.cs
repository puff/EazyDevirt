using System.CommandLine;
using System.CommandLine.Binding;

namespace EazyDevirt.Devirtualization.Options;

internal class DevirtualizationOptionsBinder : BinderBase<DevirtualizationOptions>
{
    private readonly Argument<FileInfo> _assemblyArgument;
    private readonly Argument<DirectoryInfo> _outputPathArgument;
    private readonly Option<int> _verbosityOption;
    private readonly Option<bool> _preserveAllOption;
    private readonly Option<bool> _keepTypesOption;

    public DevirtualizationOptionsBinder(Argument<FileInfo> assemblyArgument, Argument<DirectoryInfo> outputPathArgument, 
        Option<int> verbosityOption, Option<bool> preserveAllOption, Option<bool> keepTypesOption)
    {
        _assemblyArgument = assemblyArgument;
        _outputPathArgument = outputPathArgument;
        _verbosityOption = verbosityOption;
        _preserveAllOption = preserveAllOption;
        _keepTypesOption = keepTypesOption;
    }

    protected override DevirtualizationOptions GetBoundValue(BindingContext bindingContext) =>
        new DevirtualizationOptions
        {
            Assembly = bindingContext.ParseResult.GetValueForArgument(_assemblyArgument),
            OutputPath = bindingContext.ParseResult.GetValueForArgument(_outputPathArgument),
            Verbosity = bindingContext.ParseResult.GetValueForOption(_verbosityOption),
            PreserveAll = bindingContext.ParseResult.GetValueForOption(_preserveAllOption),
            KeepTypes = bindingContext.ParseResult.GetValueForOption(_keepTypesOption),
        };
}