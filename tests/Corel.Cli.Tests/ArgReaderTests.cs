using System.Globalization;

namespace Corel.Cli.Tests;

// Baseline coverage for ArgReader's flag-parsing logic AS IT EXISTS TODAY (task 21,
// 2026-09-22). Tasks 16/18 (running concurrently in their own worktrees) are explicitly
// planned to change this exact class - see ArgReader_ConsumesNextTokenAsValue_EvenWhenItLooksLikeAnotherFlag
// and ArgReader_TrailingFlagWithNoValue_YieldsEmptyString below, which lock in behavior
// those tasks intend to fix (UX-16). If either test starts failing after task 16/18
// merges, that is an expected, welcome behavior change - update/remove the test rather
// than assuming ArgReader itself regressed.
public class ArgReaderTests
{
    [Fact]
    public void Positional_CollectsTokensThatDontStartWithDoubleDash()
    {
        var reader = new ArgReader(["create-rect", "--left", "1", "somefile.cdr"]);
        Assert.Equal(["create-rect", "somefile.cdr"], reader.Positional);
    }

    [Fact]
    public void Flag_ReturnsValue_WhenPresent()
    {
        var reader = new ArgReader(["--name", "MyModule"]);
        Assert.Equal("MyModule", reader.Flag("name"));
    }

    [Fact]
    public void Flag_ReturnsNull_WhenAbsent()
    {
        var reader = new ArgReader(["--name", "MyModule"]);
        Assert.Null(reader.Flag("file"));
    }

    [Fact]
    public void Flag_LastOccurrenceWins_WhenRepeated()
    {
        var reader = new ArgReader(["--unit", "in", "--unit", "mm"]);
        Assert.Equal("mm", reader.Flag("unit"));
    }

    [Fact]
    public void ArgReader_TrailingFlagWithNoValue_YieldsEmptyString()
    {
        // Current behavior: a flag with nothing after it in the argument list gets "" as
        // its value rather than being rejected as malformed. Baseline only - not asserting
        // this is desirable.
        var reader = new ArgReader(["--force"]);
        Assert.Equal("", reader.Flag("force"));
    }

    [Fact]
    public void ArgReader_ConsumesNextTokenAsValue_EvenWhenItLooksLikeAnotherFlag()
    {
        // Current behavior (review finding UX-16, owned by task 16/18): ArgReader has no
        // concept of a boolean/known-flag token, so `--embed-vba --as out.cdr` swallows
        // "--as" as embed-vba's value and never sees a real "as" flag at all.
        var reader = new ArgReader(["--embed-vba", "--as", "out.cdr"]);
        Assert.Equal("--as", reader.Flag("embed-vba"));
        Assert.Null(reader.Flag("as"));
    }

    [Fact]
    public void Double_ParsesFlagValue()
    {
        var reader = new ArgReader(["--left", "1.5"]);
        Assert.Equal(1.5, reader.Double("left"));
    }

    [Fact]
    public void Double_ThrowsArgumentException_WhenFlagMissing()
    {
        var reader = new ArgReader([]);
        ArgumentException ex = Assert.Throws<ArgumentException>(() => reader.Double("left"));
        Assert.Contains("--left", ex.Message);
    }

    [Fact]
    public void Double_UsesInvariantCulture_RegardlessOfThreadCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // pt-BR uses ',' as the decimal separator and '.' as a thousands separator -
            // if ArgReader ever stopped passing InvariantCulture, "1.5" would either fail
            // to parse or be misread as 15.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            var reader = new ArgReader(["--left", "1.5"]);
            Assert.Equal(1.5, reader.Double("left"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void DoubleOrNull_ReturnsNull_WhenFlagMissing()
    {
        var reader = new ArgReader([]);
        Assert.Null(reader.DoubleOrNull("corner-radius"));
    }

    [Fact]
    public void DoubleOrNull_ParsesFlagValue_WhenPresent()
    {
        var reader = new ArgReader(["--corner-radius", "25"]);
        Assert.Equal(25, reader.DoubleOrNull("corner-radius"));
    }
}
