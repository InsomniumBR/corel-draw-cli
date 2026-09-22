using Corel.Cli.Commands;

namespace Corel.Cli.Tests.Commands;

public class CorelEnumsTests
{
    private static readonly Dictionary<string, string> Options = new(StringComparer.Ordinal)
    {
        ["horizontal"] = "cdrFlipHorizontal",
        ["vertical"] = "cdrFlipVertical",
    };

    [Fact]
    public void Value_ReadsTheConstantOutOfTheManifest()
        => Assert.Equal(3, CorelEnums.Value(TestManifest.Build(), "cdrFlipAxes", "cdrFlipBoth"));

    [Fact]
    public void Value_WithoutAManifest_PointsAtSchemaGenerate()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => CorelEnums.Value(null, "cdrFlipAxes", "cdrFlipBoth"));

        Assert.Contains("corel schema generate", error.Message);
    }

    [Fact]
    public void Value_UnknownEnum_NamesTheEnum()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => CorelEnums.Value(TestManifest.Build(), "cdrNoSuchEnum", "cdrThing"));

        Assert.Contains("cdrNoSuchEnum", error.Message);
    }

    [Fact]
    public void Value_UnknownMember_NamesTheMember()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => CorelEnums.Value(TestManifest.Build(), "cdrFlipAxes", "cdrFlipDiagonal"));

        Assert.Contains("cdrFlipDiagonal", error.Message);
    }

    [Fact]
    public void FromOption_MapsTheOptionWordThenTheEnumName()
        => Assert.Equal(2, CorelEnums.FromOption(TestManifest.Build(), "cdrFlipAxes", Options, "axis", "vertical"));

    [Fact]
    public void FromOption_UnknownWord_ListsTheAcceptedOnes()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => CorelEnums.FromOption(TestManifest.Build(), "cdrFlipAxes", Options, "axis", "diagonal"));

        Assert.Contains("--axis", error.Message);
        Assert.Contains("horizontal, vertical", error.Message);
    }
}

public class RgbColorTests
{
    [Theory]
    [InlineData("FF8000")]
    [InlineData("#FF8000")]
    [InlineData("ff8000")]
    public void Parse_AcceptsSixHexDigitsWithOrWithoutAHash(string value)
        => Assert.Equal(new RgbColor(255, 128, 0), RgbColor.Parse(value));

    [Theory]
    [InlineData("")]
    [InlineData("FFF")]
    [InlineData("FF80000")]
    [InlineData("GG0000")]
    [InlineData("red")]
    public void Parse_RejectsAnythingElse(string value)
        => Assert.Throws<ArgumentException>(() => RgbColor.Parse(value));

    [Fact]
    public void ToString_RoundTrips()
        => Assert.Equal("00FF7F", RgbColor.Parse("00ff7f").ToString());
}

public class ShapeTargetsParseIndexesTests
{
    [Fact]
    public void ParseIndexes_SplitsAndTrims()
        => Assert.Equal([1, 3, 5], ShapeTargets.ParseIndexes(" 1, 3 ,5 "));

    [Theory]
    [InlineData("")]
    [InlineData(",,")]
    [InlineData("0")]
    [InlineData("-2")]
    [InlineData("1,two")]
    public void ParseIndexes_RejectsAnythingThatIsNotAOneBasedIndexList(string value)
        => Assert.Throws<ArgumentException>(() => ShapeTargets.ParseIndexes(value));
}
