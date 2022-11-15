namespace EazyDevirt.Abstractions;

internal interface ILogger
{
    void Success(object message);
    void Warning(object message);
    void Error(object message);
    void Info(object message);
    void InfoStr(object message, object message2);
}