using Corel.Cli.Commands;

namespace Corel.Cli.Tests;

public class RawCommandsParseValueTests
{
    [Fact]
    public void ParseValue_MapsLiteralNullStringToNull()
    {
        Assert.Null(RawCommands.ParseValue("null"));
    }

    [Fact]
    public void ParseValue_ParsesIntegerLikeTokenAsDouble()
    {
        Assert.Equal(5d, RawCommands.ParseValue("5"));
    }

    [Fact]
    public void ParseValue_ParsesDecimalTokenAsDouble()
    {
        Assert.Equal(1.5d, RawCommands.ParseValue("1.5"));
    }

    [Fact]
    public void ParseValue_ParsesNegativeNumberAsDouble()
    {
        Assert.Equal(-2.5d, RawCommands.ParseValue("-2.5"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public void ParseValue_ParsesBooleanTokenAsBool(string raw, bool expected)
    {
        Assert.Equal(expected, RawCommands.ParseValue(raw));
    }

    [Fact]
    public void ParseValue_FallsBackToRawString_ForAnythingElse()
    {
        Assert.Equal("ActiveDocument", RawCommands.ParseValue("ActiveDocument"));
    }

    [Fact]
    public void ParseValue_TreatsEmptyStringAsRawString_NotNull()
    {
        // Only the literal "null" maps to null - an empty token (e.g. a trailing flag
        // with no value, per ArgReader's current behavior) is a distinct, non-null case.
        Assert.Equal("", RawCommands.ParseValue(""));
    }
}
