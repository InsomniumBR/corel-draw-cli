namespace Corel.Cli.Tests;

public class HelpTextTests
{
    [Fact]
    public void Overview_ListsEveryCommandInTheGrammar()
    {
        string overview = HelpText.Overview();
        foreach (CommandSpec spec in CommandGrammar.Commands)
        {
            Assert.Contains(spec.Name, overview);
        }
    }

    [Fact]
    public void ForCommand_ReturnsTheOverviewForAnEmptyTarget()
        => Assert.Equal(HelpText.Overview(), HelpText.ForCommand([]));

    [Fact]
    public void ForCommand_ReturnsNullForAnUnknownCommand()
        => Assert.Null(HelpText.ForCommand(["frobnicate"]));

    [Fact]
    public void ForCommand_ShowsRequiredAndOptionalFlags()
    {
        string help = HelpText.ForCommand(["shape", "create-rect"])!;
        Assert.Contains("--left <number>  (required)", help);
        Assert.Contains("--unit <unit>  (optional)", help);
    }

    [Fact]
    public void EveryCommandUsageLineStartsWithItsOwnName()
    {
        foreach (CommandSpec spec in CommandGrammar.Commands)
        {
            Assert.StartsWith($"corel {spec.Name}", spec.Usage);
        }
    }

    [Fact]
    public void EveryCommandUsageLineMentionsEveryFlagItAccepts()
    {
        foreach (CommandSpec spec in CommandGrammar.Commands)
        {
            foreach (FlagSpec flag in spec.Flags)
            {
                Assert.Contains($"--{flag.Name}", spec.Usage);
            }
        }
    }
}
