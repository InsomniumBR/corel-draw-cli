namespace Corel.Cli;

/// <summary>Console output helpers that keep colour handling in one place.</summary>
public static class ConsoleWriter
{
    public const ConsoleColor WarningColor = ConsoleColor.DarkYellow;

    public static void WriteWarning(string message) => WriteColored(message, WarningColor);

    private static void WriteColored(string message, ConsoleColor color)
    {
        if (Console.IsErrorRedirected)
        {
            Console.Error.WriteLine(message);
            return;
        }

        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        try
        {
            Console.Error.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
}
