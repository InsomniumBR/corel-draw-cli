namespace Corel.Cli.Tests;

public class ScriptRunnerPrepareTests
{
    private static readonly Dictionary<string, string> NoVars = [];

    [Fact]
    public void Prepare_ReportsAMissingFileWithoutTouchingAnythingElse()
    {
        ScriptPreparation preparation = ScriptRunner.Prepare(@"Z:\nope\missing.cdx", NoVars, corelVersion: null);
        Assert.Contains("Script file not found", Assert.Single(preparation.BlockingErrors));
        Assert.False(preparation.MissingVariables);
    }

    [Fact]
    public void Prepare_RejectsTheWholeRunListingEveryMissingVariable()
    {
        using var script = new TempScript("page size --width {WIDTH} --height {HEIGHT}\ndoc save --as \"{OUTPUT}\"\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(
            script.Path, new Dictionary<string, string> { ["WIDTH"] = "12" }, corelVersion: null);

        string error = Assert.Single(preparation.BlockingErrors);
        Assert.Contains("{HEIGHT}", error);
        Assert.Contains("{OUTPUT}", error);
        Assert.True(preparation.MissingVariables);
        Assert.Empty(preparation.Lines);
    }

    [Fact]
    public void Prepare_AcceptsAScriptWhoseVariablesAreAllSupplied()
    {
        using var script = new TempScript("page size --width {WIDTH} --height {HEIGHT} --unit in\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(
            script.Path, new Dictionary<string, string> { ["WIDTH"] = "12", ["HEIGHT"] = "19" }, corelVersion: null);

        Assert.Empty(preparation.BlockingErrors);
        Assert.Empty(preparation.OverridableErrors);
        Assert.Equal("page size --width 12 --height 19 --unit in", Assert.Single(preparation.Lines).Text);
    }

    [Fact]
    public void Prepare_RejectsAMalformedCommandOnAnyLineBeforeLineOneRuns()
    {
        using var script = new TempScript(
            "doc new\npage size --width 12 --height 19 --unit in\nshpae create-rect --left 0\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(script.Path, NoVars, corelVersion: null);

        string error = Assert.Single(preparation.BlockingErrors);
        Assert.StartsWith("line 3:", error);
        Assert.Contains("Unknown command 'shpae'", error);
    }

    [Fact]
    public void Prepare_ReportsEveryMalformedLineNotJustTheFirst()
    {
        using var script = new TempScript("shape delete --index abc\ndocs use\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(script.Path, NoVars, corelVersion: null);

        Assert.Equal(2, preparation.BlockingErrors.Count);
    }

    [Fact]
    public void Prepare_CountsLineNumbersAgainstTheRealFileIncludingCommentsAndBlanks()
    {
        using var script = new TempScript("# a comment\n\ndoc new\nshape nope\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(script.Path, NoVars, corelVersion: null);

        Assert.StartsWith("line 4:", Assert.Single(preparation.BlockingErrors));
    }

    [Fact]
    public void Prepare_RejectsAnUnterminatedQuoteAsABlockingError()
    {
        using var script = new TempScript("doc save --as \"C:\\out\\file.cdr\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(script.Path, NoVars, corelVersion: null);

        Assert.Contains("unterminated quote", Assert.Single(preparation.BlockingErrors));
    }

    [Fact]
    public void Prepare_RejectsAMissingMacroSourceUpFront()
    {
        using var script = new TempScript("macro install --name M --file \"Z:\\nope\\x.bas\"\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(script.Path, NoVars, corelVersion: null);

        Assert.Contains("macro source file not found", Assert.Single(preparation.BlockingErrors));
    }

    [Fact]
    public void Prepare_ReportsSafetyFindingsAsOverridableNotBlocking()
    {
        using var script = new TempScript("raw call Application.Shell cmd.exe\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(script.Path, NoVars, corelVersion: null);

        Assert.Empty(preparation.BlockingErrors);
        Assert.Contains("denylisted member 'Shell'", Assert.Single(preparation.OverridableErrors));
    }

    [Fact]
    public void Prepare_SubstitutesVariablesBeforeTheSafetyScanSoAVariableCannotSmuggleAPath()
    {
        using var script = new TempScript("doc save --as \"{OUTPUT}\"\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(
            script.Path, new Dictionary<string, string> { ["OUTPUT"] = @"C:\out\..\..\x.cdr" }, corelVersion: null);

        Assert.Contains("traversal", Assert.Single(preparation.OverridableErrors));
    }

    [Fact]
    public void Prepare_KeepsTheOriginalLineTextForEchoingIncludingQuotedSpacedPaths()
    {
        using var script = new TempScript("doc save --as \"C:\\My Folder\\out.cdr\"\n");

        ScriptPreparation preparation = ScriptRunner.Prepare(script.Path, NoVars, corelVersion: null);

        Assert.Equal("doc save --as \"C:\\My Folder\\out.cdr\"", Assert.Single(preparation.Lines).Text);
    }
}
