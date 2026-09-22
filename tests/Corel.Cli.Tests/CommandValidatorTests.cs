namespace Corel.Cli.Tests;

public class CommandValidatorTests
{
    [Theory]
    [InlineData("doc", "new")]
    [InlineData("docs", "list")]
    [InlineData("docs", "active")]
    [InlineData("shape", "list")]
    [InlineData("page", "size")]
    [InlineData("schema", "generate")]
    public void Validate_AcceptsWellFormedTwoWordCommands(string group, string sub)
        => Assert.Null(CommandValidator.Validate([group, sub]));

    [Fact]
    public void Validate_AcceptsBareSingleWordCommands()
    {
        Assert.Null(CommandValidator.Validate(["detect"]));
        Assert.Null(CommandValidator.Validate(["shell"]));
    }

    [Fact]
    public void Validate_AcceptsFullyFlaggedCreateRect()
        => Assert.Null(CommandValidator.Validate(
            ["shape", "create-rect", "--left", "0", "--top", "19", "--right", "3", "--bottom", "0",
             "--unit", "in", "--corner-radius", "10"]));

    [Fact]
    public void Validate_RejectsUnknownCommand()
    {
        string? error = CommandValidator.Validate(["shpae", "list"]);
        Assert.Contains("Unknown command 'shpae'", error);
    }

    [Fact]
    public void Validate_RejectsUnknownSubcommand()
    {
        string? error = CommandValidator.Validate(["shape", "creat-rect"]);
        Assert.Contains("Unknown shape subcommand 'creat-rect'", error);
    }

    [Fact]
    public void Validate_RejectsMissingSubcommand()
    {
        string? error = CommandValidator.Validate(["shape"]);
        Assert.Contains("needs a subcommand", error);
    }

    [Fact]
    public void Validate_RejectsMissingRequiredFlag()
    {
        string? error = CommandValidator.Validate(["shape", "create-rect", "--left", "0", "--top", "1", "--right", "1"]);
        Assert.Contains("missing required flag '--bottom'", error);
    }

    [Fact]
    public void Validate_RejectsUnknownFlag()
    {
        string? error = CommandValidator.Validate(["shape", "list", "--depth", "3"]);
        Assert.Contains("unknown flag '--depth'", error);
        Assert.Contains("it takes no flags", error);
    }

    [Fact]
    public void Validate_RejectsNonNumericNumberFlag()
    {
        string? error = CommandValidator.Validate(
            ["shape", "create-rect", "--left", "abc", "--top", "1", "--right", "1", "--bottom", "0"]);
        Assert.Contains("'--left abc' is not a number", error);
    }

    [Fact]
    public void Validate_RejectsNonIntegerIndex()
    {
        string? error = CommandValidator.Validate(["shape", "delete", "--index", "1.5"]);
        Assert.Contains("is not a whole number", error);
    }

    [Fact]
    public void Validate_RejectsUnknownUnit()
    {
        string? error = CommandValidator.Validate(["page", "size", "--width", "1", "--height", "2", "--unit", "furlong"]);
        Assert.Contains("unknown unit 'furlong'", error);
    }

    [Fact]
    public void Validate_RejectsValueFlagFollowedByAnotherFlag()
    {
        string? error = CommandValidator.Validate(["doc", "save", "--as", "--embed-vba"]);
        Assert.Contains("the next argument is another flag", error);
    }

    [Fact]
    public void Validate_RejectsValueFlagWithNoValueAtAll()
    {
        string? error = CommandValidator.Validate(["doc", "save", "--as"]);
        Assert.Contains("needs a value", error);
    }

    [Fact]
    public void Validate_BooleanFlagDoesNotSwallowTheNextToken()
        => Assert.Null(CommandValidator.Validate(["doc", "save", "--embed-vba", "--as", "out.cdr"]));

    [Fact]
    public void Validate_RejectsHalfSpecifiedPageResize()
    {
        string? error = CommandValidator.Validate(["page", "size", "--width", "5"]);
        Assert.Contains("--width, --height must be given together", error);
    }

    [Fact]
    public void Validate_RejectsRepeatedFlag()
    {
        string? error = CommandValidator.Validate(["shape", "delete", "--index", "1", "--index", "2"]);
        Assert.Contains("given more than once", error);
    }

    [Fact]
    public void Validate_RejectsTooManyPositionals()
    {
        string? error = CommandValidator.Validate(["docs", "use", "a", "b"]);
        Assert.Contains("expected at most 1", error);
    }

    [Fact]
    public void Validate_RejectsMissingPositional()
    {
        string? error = CommandValidator.Validate(["docs", "use"]);
        Assert.Contains("expected at least 1", error);
    }

    [Fact]
    public void Validate_ChecksRawsOwnGrammarOnly()
    {
        Assert.Null(CommandValidator.Validate(["raw", "get", "ActiveDocument.Name"]));
        Assert.Null(CommandValidator.Validate(["raw", "set", "ActiveDocument.Unit", "1"]));
        Assert.Null(CommandValidator.Validate(
            ["raw", "call", "ActiveDocument.ActiveLayer.CreateRectangle", "0", "1", "1", "0", "null", "null", "null", "null"]));

        Assert.Contains("Unknown raw subcommand 'delete'", CommandValidator.Validate(["raw", "delete", "X"]));
        Assert.Contains("expected at least 2", CommandValidator.Validate(["raw", "set", "ActiveDocument.Unit"]));
    }

    [Fact]
    public void Validate_DoesNotInspectRawCallArgumentsThatLookLikeFlags()
        => Assert.Null(CommandValidator.Validate(["raw", "call", "SomeMember", "--not-a-flag"]));

    [Fact]
    public void ValidateResources_RejectsMissingMacroSource()
    {
        string? error = CommandValidator.ValidateResources(
            ["macro", "install", "--name", "M", "--file", @"Z:\definitely\missing.bas"]);
        Assert.Contains("macro source file not found", error);
    }

    [Fact]
    public void ValidateResources_PassesForExistingMacroSource()
    {
        using var file = new TempScript("Sub Hello()\nEnd Sub\n", ".bas");
        Assert.Null(CommandValidator.ValidateResources(["macro", "install", "--name", "M", "--file", file.Path]));
    }

    [Fact]
    public void Parse_KeepsBooleanAndValueFlagsApart()
    {
        CommandSpec spec = CommandGrammar.Find(["script", "run"])!;
        ParsedArgs parsed = CommandValidator.Parse(spec, ["a.cdx", "--force", "--vars", "OUTPUT=x"]);

        Assert.Equal("a.cdx", parsed.Positional[0]);
        Assert.True(parsed.Has("force"));
        Assert.Equal("OUTPUT=x", parsed.Value("vars"));
    }
}
