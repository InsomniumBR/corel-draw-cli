using Corel.Cli.Commands;

namespace Corel.Cli.Tests;

public class RawCommandsTests
{
    private sealed class FakeDocument
    {
        public double SizeWidth => 12.0;
    }

    private sealed class FakeApp
    {
        public object? ActiveDocument { get; set; }
    }

    [Fact]
    public void Run_MissingArgs_PrintsUsageAndActionWarning()
    {
        (int exitCode, _, string error) = RunCaptured(["get"], new FakeApp());

        Assert.Equal(1, exitCode);
        Assert.Contains("Usage: corel raw", error);
        Assert.Contains("raw get", error);
    }

    [Fact]
    public void Run_Get_NoActiveDocument_PrintsRemedyHint_WithoutSchemaHint()
    {
        var app = new FakeApp { ActiveDocument = null };

        (int exitCode, _, string error) = RunCaptured(["get", "ActiveDocument.SizeWidth"], app);

        Assert.Equal(1, exitCode);
        Assert.Contains("corel doc new", error);
        Assert.DoesNotContain("schema generate", error);
    }

    [Fact]
    public void Run_Get_UnknownMember_IncludesSchemaGenerateHint()
    {
        var app = new FakeApp { ActiveDocument = new FakeDocument() };

        (int exitCode, _, string error) = RunCaptured(["get", "ActiveDocument.NoSuchMember"], app);

        Assert.Equal(1, exitCode);
        Assert.Contains("schema generate", error);
    }

    [Fact]
    public void Run_Get_Success_PrintsValueWithNoError()
    {
        var app = new FakeApp { ActiveDocument = new FakeDocument() };

        (int exitCode, string output, string error) = RunCaptured(["get", "ActiveDocument.SizeWidth"], app);

        Assert.Equal(0, exitCode);
        Assert.Contains("12", output);
        Assert.Empty(error);
    }

    private static (int ExitCode, string Output, string Error) RunCaptured(string[] args, object app)
    {
        var outWriter = new StringWriter();
        var errWriter = new StringWriter();
        TextWriter originalOut = Console.Out;
        TextWriter originalErr = Console.Error;
        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            int exitCode = RawCommands.Run(args, app);
            return (exitCode, outWriter.ToString(), errWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }
}
