namespace Corel.Cli.Tests;

/// <summary>Swaps Console.Error for the duration of an action and returns what it wrote.</summary>
internal static class ConsoleCapture
{
    public static string StdErr(Action action)
    {
        TextWriter original = Console.Error;
        var writer = new StringWriter();
        Console.SetError(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetError(original);
        }
        return writer.ToString();
    }
}
