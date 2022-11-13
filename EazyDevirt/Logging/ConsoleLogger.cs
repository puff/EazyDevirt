using EazyDevirt.Abstractions;

namespace EazyDevirt.Logging;

public class ConsoleLogger : ILogger
{
    public void Success(object message) => WriteLine(message, ConsoleColor.Cyan, '+');

    public void Warning(object message) => WriteLine(message, ConsoleColor.Yellow, '-');

    public void Error(object message) => WriteLine(message, ConsoleColor.Red, '!');

    public void Info(object message) => WriteLine(message, ConsoleColor.Gray, '*');

    public void InfoStr(object message, object message2) => WriteLineInfo(message, ConsoleColor.Red, message2);
    
    private void WriteLine(object message, ConsoleColor color, char character)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("[");
        Console.ForegroundColor = color;
        Console.Write(character);
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("] ");
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void WriteLineInfo(object message, ConsoleColor color, object msg2)
    {
        Console.ForegroundColor = color;
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(msg2);
        Console.ForegroundColor = color;
        Console.Write("] ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}