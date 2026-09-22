namespace Corel.Cli.Tests;

public class ScriptVarsTests
{
    [Fact]
    public void Parse_SplitsKeyValuePairs()
    {
        Dictionary<string, string> vars = ScriptVars.Parse("OUTPUT=file.cdr,WIDTH=12,HEIGHT=19");
        Assert.Equal("file.cdr", vars["OUTPUT"]);
        Assert.Equal("12", vars["WIDTH"]);
        Assert.Equal("19", vars["HEIGHT"]);
    }

    [Fact]
    public void Parse_SplitsOnlyOnFirstEquals_SoValueMayContainEquals()
    {
        Dictionary<string, string> vars = ScriptVars.Parse("EXPR=a=b");
        Assert.Equal("a=b", vars["EXPR"]);
    }

    [Fact]
    public void Parse_ReturnsEmpty_ForNullOrEmpty()
    {
        Assert.Empty(ScriptVars.Parse(null));
        Assert.Empty(ScriptVars.Parse(""));
    }

    [Fact]
    public void Validate_ReturnsNull_WhenNoPlaceholdersUsed()
    {
        string[] lines = ["doc new", "shape list"];
        Assert.Null(ScriptVars.Validate(lines, new Dictionary<string, string>()));
    }

    [Fact]
    public void Validate_ReturnsError_ListingEveryMissingVariable()
    {
        string[] lines = ["page size --width {WIDTH} --height {HEIGHT}", "doc save --as \"{OUTPUT}\""];
        string? error = ScriptVars.Validate(lines, new Dictionary<string, string> { ["WIDTH"] = "12" });
        Assert.NotNull(error);
        Assert.Contains("{HEIGHT}", error);
        Assert.Contains("{OUTPUT}", error);
        Assert.DoesNotContain("{WIDTH}", error);
    }

    [Fact]
    public void Validate_ReturnsNull_WhenEveryPlaceholderIsProvided()
    {
        string[] lines = ["doc save --as \"{OUTPUT}\""];
        Assert.Null(ScriptVars.Validate(lines, new Dictionary<string, string> { ["OUTPUT"] = @"C:\out.cdr" }));
    }

    [Fact]
    public void Apply_ReplacesEveryPlaceholderWithItsValue()
    {
        string[] lines = ["page size --width {WIDTH} --height {HEIGHT} --unit in", "doc save --as \"{OUTPUT}\""];
        var vars = new Dictionary<string, string> { ["WIDTH"] = "12", ["HEIGHT"] = "19", ["OUTPUT"] = @"C:\out.cdr" };

        string[] result = [.. ScriptVars.Apply(lines, vars)];

        Assert.Equal("page size --width 12 --height 19 --unit in", result[0]);
        Assert.Equal("doc save --as \"C:\\out.cdr\"", result[1]);
    }

    [Fact]
    public void Apply_LeavesUnknownPlaceholdersUntouched()
    {
        string[] lines = ["{UNKNOWN}"];
        string[] result = [.. ScriptVars.Apply(lines, new Dictionary<string, string>())];
        Assert.Equal("{UNKNOWN}", result[0]);
    }
}
