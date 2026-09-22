namespace Corel.Cli.Tests;

[Collection("console")]
public class ConsoleWriterTests
{
    [Fact]
    public void WarningColorIsDarkYellow()
        => Assert.Equal(ConsoleColor.DarkYellow, ConsoleWriter.WarningColor);

    [Fact]
    public void WriteWarning_WritesTheMessageToStandardError()
    {
        using var console = new ConsoleCapture();
        ConsoleWriter.WriteWarning("This script references undefined variable(s): {OUTPUT}.");
        Assert.Contains("{OUTPUT}", console.Error);
    }
}
