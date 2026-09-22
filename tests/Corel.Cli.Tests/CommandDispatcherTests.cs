using Corel.Cli;

namespace Corel.Cli.Tests;

// Routing-only coverage for CommandDispatcher.Dispatch as it exists TODAY (task 21,
// 2026-09-22). Task 16 (running concurrently in its own worktree) is planned to change
// dispatch order specifically around --help and unknown commands (review finding UX-2) -
// Dispatch_UnknownCommand_StillCallsConnect_ThenReturnsError below locks in the CURRENT
// (undesirable) ordering and is expected to need re-checking, possibly removal, once
// task 16 merges. The other tests here (routing table + detect/schema connect-laziness)
// are not something task 16 intends to change and should keep passing.
//
// "detect" is deliberately NOT exercised here: DetectCommand.Run always performs a real
// registry scan and overwrites the real %LOCALAPPDATA%\corel-draw-cli\installs.json,
// which this project's own testing convention (AGENTS.md "Tests" section: pure logic
// only) argues against. "schema" with no further args hits the exact same
// early-return-before-connect() code shape with no side effect at all, and stands in as
// the representative case for that rule instead.
public class CommandDispatcherTests
{
    [Fact]
    public void Dispatch_EmptyArgs_ReturnsUsageError_WithoutCallingConnect()
    {
        bool connectCalled = false;
        (object, string?) Connect() { connectCalled = true; return (new object(), null); }

        int result = -1;
        string stderr = ConsoleCapture.StdErr(() => result = CommandDispatcher.Dispatch([], Connect));

        Assert.Equal(1, result);
        Assert.False(connectCalled);
        Assert.Contains("Usage: corel <command> [args...]", stderr);
    }

    [Fact]
    public void Dispatch_Schema_RoutesToSchemaCommands_WithoutCallingConnect()
    {
        bool connectCalled = false;
        (object, string?) Connect() { connectCalled = true; return (new object(), null); }

        int result = -1;
        string stderr = ConsoleCapture.StdErr(() => result = CommandDispatcher.Dispatch(["schema"], Connect));

        Assert.Equal(1, result);
        Assert.False(connectCalled);
        Assert.Contains("Usage: corel schema generate [--version N]", stderr);
    }

    [Fact]
    public void Dispatch_UnknownCommand_StillCallsConnect_ThenReturnsError()
    {
        // Baseline/current behavior only (review finding UX-2, owned by task 16): the
        // dispatcher connects to COM BEFORE checking whether the command is even known.
        bool connectCalled = false;
        (object, string?) Connect() { connectCalled = true; return (new object(), null); }

        int result = -1;
        string stderr = ConsoleCapture.StdErr(() => result = CommandDispatcher.Dispatch(["bogus"], Connect));

        Assert.Equal(1, result);
        Assert.True(connectCalled);
        Assert.Contains("Unknown command 'bogus'.", stderr);
    }

    [Theory]
    [InlineData("raw", "Usage: corel raw <get|set|call> <path> [args...]")]
    [InlineData("page", "Usage: corel page size [--width W --height H] [--unit in|mm|...]")]
    [InlineData("shape", "Usage: corel shape <create-rect|list|delete> ...")]
    [InlineData("docs", "Usage: corel docs <list|use|active>")]
    [InlineData("doc", "Usage: corel doc <new|save>")]
    [InlineData("macro", "Usage: corel macro install --name <ModuleName> --file <path.bas>")]
    public void Dispatch_RoutesKnownCommandName_ToItsOwnHandler(string command, string expectedUsageMessage)
    {
        // Each handler's own "missing sub-args" branch fires before it ever touches the
        // `app` object, so a plain placeholder object is enough to prove routing without
        // needing a real (or fake) COM object.
        bool connectCalled = false;
        (object, string?) Connect() { connectCalled = true; return (new object(), null); }

        int result = -1;
        string stderr = ConsoleCapture.StdErr(() => result = CommandDispatcher.Dispatch([command], Connect));

        Assert.Equal(1, result);
        Assert.True(connectCalled);
        Assert.Contains(expectedUsageMessage, stderr);
    }
}
