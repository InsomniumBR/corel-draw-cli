namespace Corel.Cli.Tests;

[Collection("console")]
public class ReplLoopTests
{
    [Fact]
    public void RunLoop_ReturnsTheLastCommandsExitCode()
    {
        using var console = new ConsoleCapture();
        int result = Repl.RunLoop(new StringReader("shape list\ndoc new\nexit\n"), _ => 7);
        Assert.Equal(7, result);
    }

    [Fact]
    public void RunLoop_ReturnsZeroWhenNoCommandRan()
    {
        using var console = new ConsoleCapture();
        Assert.Equal(0, Repl.RunLoop(new StringReader("exit\n"), _ => 5));
    }

    [Fact]
    public void RunLoop_ReportsAThrownCommandAsExitCodeOneAndKeepsGoing()
    {
        using var console = new ConsoleCapture();
        var seen = new List<string>();

        int result = Repl.RunLoop(new StringReader("boom\nfine\n"), tokens =>
        {
            seen.Add(tokens[0]);
            return tokens[0] == "boom" ? throw new InvalidOperationException("nope") : 0;
        });

        Assert.Equal(["boom", "fine"], seen);
        Assert.Equal(0, result);
        Assert.Contains("nope", console.Error);
    }

    [Fact]
    public void RunLoop_ReportsAnUnterminatedQuoteWithoutDispatching()
    {
        using var console = new ConsoleCapture();
        var dispatched = 0;

        int result = Repl.RunLoop(new StringReader("doc save --as \"unclosed\n"), _ => { dispatched++; return 0; });

        Assert.Equal(0, dispatched);
        Assert.Equal(1, result);
        Assert.Contains("unterminated quote", console.Error);
    }

    [Fact]
    public void RunLoop_IgnoresAByteOrderMarkOnAPipedFirstLine()
    {
        using var console = new ConsoleCapture();
        var seen = new List<string>();

        Repl.RunLoop(new StringReader("﻿docs list\n﻿exit\n"), tokens => { seen.Add(tokens[0]); return 0; });

        Assert.Equal(["docs"], seen);
    }

    [Fact]
    public void RunLoop_SkipsBlankAndCommentLines()
    {
        using var console = new ConsoleCapture();
        var dispatched = 0;
        Repl.RunLoop(new StringReader("\n   \n# a comment\nexit\n"), _ => { dispatched++; return 0; });
        Assert.Equal(0, dispatched);
    }
}
