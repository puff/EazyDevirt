using EazyDevirt.Abstractions;

namespace EazyDevirt.Logging;

public class ConsoleLogger : ILogger
{
    public void Success(string message) => WriteLine(message, ConsoleColor.Cyan, '+');

    public void Warning(string message) => WriteLine(message, ConsoleColor.Yellow, '-');

    public void Error(string message) => WriteLine(message, ConsoleColor.Red, '!');

    public void Info(string message) => WriteLine(message, ConsoleColor.Gray, '*');

    public void InfoStr(string message, string message2) => WriteLineInfo(message, ConsoleColor.Red, message2);


    private void WriteLine(string message, ConsoleColor color, char character)
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

    private void WriteLineInfo(string message, ConsoleColor color, string msg2)
    {
        Console.ForegroundColor = color;
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(msg2, ConsoleColor.White);
        Console.ForegroundColor = color;
        Console.Write("] ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}