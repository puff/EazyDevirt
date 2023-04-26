using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.Logging;

internal  class ConsoleLogger : ILogger
{
    public void Success(object message) => WriteLine(message, ConsoleColor.Cyan, '+');

    public void Warning(object message) => WriteLine(message, ConsoleColor.Yellow, '-');

    public void Error(object message) => WriteLine(message, ConsoleColor.Red, '!');

    public void Info(object message) => WriteLine(message, ConsoleColor.Gray, '*');

    public void InfoStr(object message, object message2) => WriteLineInfo(message, ConsoleColor.Red, message2);
    
    public void ShowInfo(Version version, Version eazVersion)
    {
        Console.WriteLine();
        Console.WriteLine();
        WriteLineMiddle(@"                ▄███▄   ██   ▄▄▄▄▄▄ ▀▄    ▄                              ", ConsoleColor.DarkMagenta);
        WriteLineMiddle(@"                █▀   ▀  █ █ ▀   ▄▄▀   █  █                               ", ConsoleColor.DarkMagenta);
        WriteLineMiddle(@"                ██▄▄    █▄▄█ ▄▀▀   ▄▀  ▀█        _            _      _   ", ConsoleColor.DarkMagenta);
        WriteLineMiddle(@"                █▄   ▄▀ █  █ ▀▀▀▀▀▀    █      __| | _____   _(_)_ __| |_ ", ConsoleColor.DarkMagenta);
        WriteLineMiddle(@"                ▀███▀      █         ▄▀      / _` |/ _ \ \ / / | '__| __|", ConsoleColor.DarkMagenta);
        WriteLineMiddle(@"                          █                 | (_| |  __/\ V /| | |  | |_ ", ConsoleColor.DarkMagenta);
        WriteLineMiddle(@"                         ▀                   \__,_|\___| \_/ |_|_|   \__|", ConsoleColor.DarkMagenta);
        Console.WriteLine();
        
        WriteMiddle(@"Version - ", ConsoleColor.DarkMagenta);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(version);
        Console.ResetColor();
        
        WriteMiddle(@"Eazfuscator.NET Version - ", ConsoleColor.DarkMagenta);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(eazVersion);
        Console.ResetColor();
        
        WriteMiddle(@"Developers - ", ConsoleColor.DarkMagenta);
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("puff");
        Console.ResetColor();

        WriteMiddle(@"Github Repo - ", ConsoleColor.DarkMagenta);
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("https://github.com/puff/EazyDevirt");
        Console.ResetColor();
        
        WriteMiddle(@"Thanks to - ", ConsoleColor.DarkMagenta);
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("void-stack for the many contributions."); 
        
        // 55 = ("saneki for the well-documented eazdevirt project.".length + "Thanks to - ".length) - ("Thanks to - ".length / 2)
        Console.WriteLine(string.Format("{0," + (Console.WindowWidth / 2
                                                 + 55 + "}"), "saneki for the well-documented eazdevirt project."));
        
        // 47 = ("TobitoFatitoRE for the amazing HexDevirt project.".length + "Thanks to - ".length) - ("Thanks to - ".length / 2)
        Console.WriteLine(string.Format("{0," + (Console.WindowWidth / 2 
                                                 + 47 + "}"), "TobitoFatitoRE for the HexDevirt project."));
        
        // 54 = ("Washi1337 for the wonderful AsmResolver library.".length + "Thanks to - ".length) - ("Thanks to - ".length / 2)
        Console.WriteLine(string.Format("{0," + (Console.WindowWidth / 2
                                                 + 54 + "}"), "Washi1337 for the wonderful AsmResolver library."));
        Console.ResetColor();
    }

    private void WriteMiddle(object message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(string.Format("{0," + (Console.WindowWidth / 2 + message.ToString()?.Length / 2) + "}",
            message));
        Console.ResetColor();
    }

    private void WriteLineMiddle(object message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(string.Format("{0," + (Console.WindowWidth / 2 + message.ToString()?.Length / 2) + "}",
            message));
        Console.ResetColor();
    }
    
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