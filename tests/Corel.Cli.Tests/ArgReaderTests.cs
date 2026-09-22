using System.Globalization;

namespace Corel.Cli.Tests;

public class ArgReaderTests
{
    [Fact]
    public void Flag_ReturnsValue_ForFlagFollowedByAValue()
    {
        var reader = new ArgReader(["--left", "5"]);
        Assert.Equal("5", reader.Flag("left"));
    }

    [Fact]
    public void Flag_ReturnsNull_ForFlagNotPresentAtAll()
    {
        var reader = new ArgReader(["--left", "5"]);
        Assert.Null(reader.Flag("top"));
    }

    [Fact]
    public void Flag_LastOccurrenceWins_WhenRepeated()
    {
        var reader = new ArgReader(["--unit", "in", "--unit", "mm"]);
        Assert.Equal("mm", reader.Flag("unit"));
    }

    [Fact]
    public void BooleanStyleFlag_ReturnsEmptyString_NotNull()
    {
        // Presence-check idiom used by callers like DocCommands: `Flag("embed-vba") is not null`.
        var reader = new ArgReader(["--embed-vba"]);
        Assert.NotNull(reader.Flag("embed-vba"));
        Assert.Equal("", reader.Flag("embed-vba"));
    }

    [Fact]
    public void BooleanStyleFlag_DoesNotSwallowTheNextFlagAsItsValue()
    {
        // Regression test for the exact bug reported: `doc save --embed-vba --as out.cdr`
        // used to consume "--as" as embed-vba's value, leaving --as unset.
        var reader = new ArgReader(["--embed-vba", "--as", "out.cdr"]);
        Assert.Equal("", reader.Flag("embed-vba"));
        Assert.Equal("out.cdr", reader.Flag("as"));
    }

    [Fact]
    public void FlagWithMissingValue_DoesNotStealTheFollowingFlagsName()
    {
        // Regression test for the exact bug reported: `--left --top 5` used to yield
        // left="--top" instead of leaving --left unset and --top=5.
        var reader = new ArgReader(["--left", "--top", "5"]);
        Assert.Equal("", reader.Flag("left"));
        Assert.Equal("5", reader.Flag("top"));
    }

    [Fact]
    public void DoubleOrNull_ParsesAProvidedValue()
    {
        var reader = new ArgReader(["--left", "5.5"]);
        Assert.Equal(5.5, reader.DoubleOrNull("left"));
    }

    [Fact]
    public void DoubleOrNull_ReturnsNull_WhenFlagIsEntirelyAbsent()
    {
        var reader = new ArgReader([]);
        Assert.Null(reader.DoubleOrNull("left"));
    }

    [Fact]
    public void DoubleOrNull_ReturnsNull_WhenFlagsValueWasSwallowedByAnotherFlag()
    {
        var reader = new ArgReader(["--left", "--top", "5"]);
        Assert.Null(reader.DoubleOrNull("left"));
    }

    [Fact]
    public void DoubleOrNull_UsesInvariantCulture_RegardlessOfThreadCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // pt-BR uses ',' as the decimal separator and '.' as a thousands separator -
            // if ArgReader ever stopped passing InvariantCulture, "1.5" would either fail
            // to parse or be misread as 15.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            var reader = new ArgReader(["--left", "1.5"]);
            Assert.Equal(1.5, reader.DoubleOrNull("left"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Positional_CollectsNonFlagTokens()
    {
        var reader = new ArgReader(["install", "--name", "Foo"]);
        Assert.Equal(["install"], reader.Positional);
    }

    [Fact]
    public void NegativeNumberValue_IsNotTreatedAsAFlag()
    {
        var reader = new ArgReader(["--left", "-5"]);
        Assert.Equal(-5, reader.DoubleOrNull("left"));
    }
}
