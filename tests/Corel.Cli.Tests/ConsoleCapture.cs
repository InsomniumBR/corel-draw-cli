using System.Text;

namespace Corel.Cli.Tests;

/// <summary>Console redirection is process-global, so every test that captures it shares
/// one non-parallel collection.</summary>
[CollectionDefinition("console")]
public class ConsoleCollection;

public sealed class ConsoleCapture : IDisposable
{
    private readonly TextWriter _previousOut = Console.Out;
    private readonly TextWriter _previousError = Console.Error;
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();

    public ConsoleCapture()
    {
        Console.SetOut(_out);
        Console.SetError(_error);
    }

    public string Out => _out.ToString();

    public string Error => _error.ToString();

    public void Dispose()
    {
        Console.SetOut(_previousOut);
        Console.SetError(_previousError);
        _out.Dispose();
        _error.Dispose();
    }
}

public sealed class TempScript : IDisposable
{
    public TempScript(string content, string extension = ".cdx")
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"corel-test-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(Path, content, Encoding.UTF8);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
