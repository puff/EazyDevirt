using System.CommandLine;
using System.CommandLine.Binding;

namespace EazyDevirt.Devirtualization.Options;

internal class DevirtualizationOptionsBinder : BinderBase<DevirtualizationOptions>
{
    private readonly Argument<FileInfo> _assemblyArgument;
    private readonly Argument<DirectoryInfo> _outputPathArgument;
    private readonly Option<int> _verbosityOption;
    private readonly Option<bool> _noVerifyOption;
    private readonly Option<bool> _preserveAllOption;
    private readonly Option<bool> _keepTypesOption;
    private readonly Option<bool> _saveAnywayOption;
    private readonly Option<bool> _onlySaveDevirtedOption;

    public DevirtualizationOptionsBinder(Argument<FileInfo> assemblyArgument, Argument<DirectoryInfo> outputPathArgument, 
        Option<int> verbosityOption, Option<bool> preserveAllOption, Option<bool> noVerifyOption, Option<bool> keepTypesOption, Option<bool> saveAnywayOption,
        Option<bool> onlySaveDevirtedOption)
    {
        _assemblyArgument = assemblyArgument;
        _outputPathArgument = outputPathArgument;
        _verbosityOption = verbosityOption;
        _preserveAllOption = preserveAllOption;
        _noVerifyOption = noVerifyOption;
        _keepTypesOption = keepTypesOption;
        _saveAnywayOption = saveAnywayOption;
        _onlySaveDevirtedOption = onlySaveDevirtedOption;
    }

    protected override DevirtualizationOptions GetBoundValue(BindingContext bindingContext) =>
        new()
        {
            Assembly = bindingContext.ParseResult.GetValueForArgument(_assemblyArgument),
            OutputPath = bindingContext.ParseResult.GetValueForArgument(_outputPathArgument),
            Verbosity = bindingContext.ParseResult.GetValueForOption(_verbosityOption),
            PreserveAll = bindingContext.ParseResult.GetValueForOption(_preserveAllOption),
            NoVerify = bindingContext.ParseResult.GetValueForOption(_noVerifyOption),
            KeepTypes = bindingContext.ParseResult.GetValueForOption(_keepTypesOption),
            SaveAnyway = bindingContext.ParseResult.GetValueForOption(_saveAnywayOption),
            OnlySaveDevirted = bindingContext.ParseResult.GetValueForOption(_onlySaveDevirtedOption)
        };
}