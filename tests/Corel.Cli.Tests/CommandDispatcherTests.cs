namespace Corel.Cli.Tests;

[Collection("console")]
public class CommandDispatcherTests
{
    /// <summary>A session that fails loudly if anything tries to attach to CorelDRAW.</summary>
    private static CorelSession NeverConnects()
        => new("17", () => throw new InvalidOperationException("this path must not connect to CorelDRAW"));

    [Fact]
    public void Dispatch_ShowsOverviewHelpWithoutConnecting()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(0, CommandDispatcher.Dispatch(["help"], NeverConnects()));
        Assert.Contains("shape create-rect", console.Out);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Dispatch_TreatsTopLevelHelpFlagsAsHelp(string flag)
    {
        using var console = new ConsoleCapture();
        Assert.Equal(0, CommandDispatcher.Dispatch([flag], NeverConnects()));
        Assert.Contains("Usage: corel", console.Out);
    }

    [Fact]
    public void Dispatch_ShowsPerCommandHelpForACommandFlaggedWithHelp()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(0, CommandDispatcher.Dispatch(["shape", "create-rect", "--help"], NeverConnects()));
        Assert.Contains("--corner-radius", console.Out);
    }

    [Fact]
    public void Dispatch_ShowsEveryCommandInAGroupForGroupHelp()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(0, CommandDispatcher.Dispatch(["help", "docs"], NeverConnects()));
        Assert.Contains("corel docs use", console.Out);
        Assert.Contains("corel docs active", console.Out);
    }

    [Fact]
    public void Dispatch_RejectsHelpForAnUnknownCommand()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(1, CommandDispatcher.Dispatch(["help", "nonsense"], NeverConnects()));
        Assert.Contains("Unknown command 'nonsense'", console.Error);
    }

    [Fact]
    public void Dispatch_RejectsAnUnknownCommandWithoutConnecting()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(1, CommandDispatcher.Dispatch(["frobnicate"], NeverConnects()));
        Assert.Contains("Unknown command 'frobnicate'", console.Error);
    }

    [Fact]
    public void Dispatch_RejectsAMalformedKnownCommandWithoutConnecting()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(1, CommandDispatcher.Dispatch(["shape", "create-rect", "--left", "0"], NeverConnects()));
        Assert.Contains("missing required flag", console.Error);
    }

    [Fact]
    public void Dispatch_RoutesScriptRunSoTheShellCanRunScriptsToo()
    {
        using var console = new ConsoleCapture();
        using var script = new TempScript("shpae list\n");

        Assert.Equal(1, CommandDispatcher.Dispatch(["script", "run", script.Path], NeverConnects()));

        Assert.Contains("Unknown command 'shpae'", console.Error);
    }

    [Fact]
    public void Dispatch_ValidatesAScriptBeforeConnecting()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(1, CommandDispatcher.Dispatch(["script", "run", @"Z:\nope\missing.cdx"], NeverConnects()));
        Assert.Contains("Script file not found", console.Error);
    }

    [Fact]
    public void Dispatch_RejectsScriptRunWithNoFileArgument()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(1, CommandDispatcher.Dispatch(["script", "run"], NeverConnects()));
        Assert.Contains("expected at least 1", console.Error);
    }

    [Fact]
    public void Dispatch_RefusesToStartAShellFromInsideASession()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(1, CommandDispatcher.Dispatch(["shell"], NeverConnects()));
        Assert.Contains("interactive session", console.Error);
    }

    [Fact]
    public void Dispatch_SafetyViolationsAbortAScriptUnlessForced()
    {
        using var console = new ConsoleCapture();
        using var script = new TempScript("raw call Application.Shell cmd.exe\n");

        Assert.Equal(1, CommandDispatcher.Dispatch(["script", "run", script.Path], NeverConnects()));

        Assert.Contains("Preflight failed", console.Error);
        Assert.Contains("Pass --force to run anyway", console.Error);
    }

    [Fact]
    public void Dispatch_ForceWaivesSafetyAndProceedsToExecution()
    {
        using var console = new ConsoleCapture();
        using var script = new TempScript("raw call Application.Shell cmd.exe\n");

        Assert.Equal(1, CommandDispatcher.Dispatch(["script", "run", script.Path, "--force"], NeverConnects()));

        Assert.Contains("--force given: running anyway.", console.Error);
        Assert.Contains("this path must not connect to CorelDRAW", console.Error);
    }

    [Fact]
    public void Dispatch_ForceNeverWaivesAMalformedCommand()
    {
        using var console = new ConsoleCapture();
        using var script = new TempScript("shape delete --index abc\n");

        Assert.Equal(1, CommandDispatcher.Dispatch(["script", "run", script.Path, "--force"], NeverConnects()));

        Assert.Contains("is not a whole number", console.Error);
    }
}
