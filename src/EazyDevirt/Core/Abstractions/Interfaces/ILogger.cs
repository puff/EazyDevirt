using EazyDevirt.Logging;

namespace EazyDevirt.Core.Abstractions.Interfaces;

internal interface ILogger
{
    void Success(object message, VerboseLevel verbosityLevel = 0);
    void Warning(object message, VerboseLevel verbosityLevel = 0);
    void Error(object message, VerboseLevel verbosityLevel = 0);
    void Info(object message, VerboseLevel verbosityLevel = 0);
    void InfoStr(object message, object message2, VerboseLevel verbosityLevel = 0);
}