using System.CommandLine;
using System.CommandLine.Binding;
using EazyDevirt.Logging;

namespace EazyDevirt.Core;

internal class OptionsBinder : BinderBase<Options>
{
    private readonly Argument<FileInfo> _assemblyArgument;
    private readonly Option<bool> _keepTypesOption;
    private readonly Option<bool> _onlySaveDevirtedOption;
    private readonly Argument<DirectoryInfo> _outputPathArgument;
    private readonly Option<bool> _preserveAllOption;
    private readonly Option<bool> _saveAnywayOption;
    private readonly Option<VerboseLevel> _verbosityOption;

    public OptionsBinder(Argument<FileInfo> assemblyArgument, Argument<DirectoryInfo> outputPathArgument,
        Option<VerboseLevel> verbosityOption, Option<bool> preserveAllOption, Option<bool> keepTypesOption,
        Option<bool> saveAnywayOption,
        Option<bool> onlySaveDevirtedOption)
    {
        _assemblyArgument = assemblyArgument;
        _outputPathArgument = outputPathArgument;
        _verbosityOption = verbosityOption;
        _preserveAllOption = preserveAllOption;
        _keepTypesOption = keepTypesOption;
        _saveAnywayOption = saveAnywayOption;
        _onlySaveDevirtedOption = onlySaveDevirtedOption;
    }

    protected override Options GetBoundValue(BindingContext bindingContext)
    {
        return new Options
        {
            Assembly = bindingContext.ParseResult.GetValueForArgument(_assemblyArgument),
            OutputPath = bindingContext.ParseResult.GetValueForArgument(_outputPathArgument),
            Verbosity = bindingContext.ParseResult.GetValueForOption(_verbosityOption),
            PreserveAll = bindingContext.ParseResult.GetValueForOption(_preserveAllOption),
            KeepTypes = bindingContext.ParseResult.GetValueForOption(_keepTypesOption),
            SaveAnyway = bindingContext.ParseResult.GetValueForOption(_saveAnywayOption),
            OnlySaveDevirted = bindingContext.ParseResult.GetValueForOption(_onlySaveDevirtedOption)
        };
    }
}