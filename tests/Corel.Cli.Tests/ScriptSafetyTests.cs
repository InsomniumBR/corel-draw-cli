namespace Corel.Cli.Tests;

public class ScriptSafetyTests
{
    private static IReadOnlyList<SafetyViolation> Inspect(params string[] lines)
        => ScriptSafety.Inspect([.. lines.Select((text, i) => new ScriptLine(i + 1, text, Repl.Tokenize(text)))]);

    [Fact]
    public void Inspect_AllowsOrdinaryDrawingCommands()
        => Assert.Empty(Inspect(
            "doc new",
            "page size --width 12 --height 19 --unit in",
            "shape create-rect --left 0 --top 19 --right 3 --bottom 0",
            "raw get ActiveDocument.ActivePage.SizeWidth",
            "raw call ActiveDocument.ActiveLayer.CreateRectangle 0 1 1 0 null null null null",
            @"doc save --as ""C:\out\file.cdr"""));

    [Theory]
    [InlineData("raw call Application.Shell cmd.exe", "Shell")]
    [InlineData("raw call CreateObject WScript.Shell", "CreateObject")]
    [InlineData("raw call Application.Kill somefile", "Kill")]
    [InlineData("raw set Application.RegWrite 1", "RegWrite")]
    [InlineData("raw get Application.VBE.VBProjects.Count", "VBE")]
    [InlineData("raw call ActiveDocument.GMSManager.RunMacro x", "RunMacro")]
    public void Inspect_BlocksDenylistedRawPathSegments(string line, string expected)
    {
        IReadOnlyList<SafetyViolation> violations = Inspect(line);
        Assert.Contains(violations, v => v.Reason.Contains(expected));
    }

    [Fact]
    public void Inspect_MatchesWholeSegmentsSoSimilarlyNamedMembersArePermitted()
        => Assert.Empty(Inspect("raw get ActiveDocument.ShellSortOrder", "raw get ActiveDocument.Killer"));

    [Fact]
    public void Inspect_BlocksShellAutomationProgIdPassedAsAnArgument()
    {
        IReadOnlyList<SafetyViolation> violations = Inspect("raw call ActiveDocument.Something Scripting.FileSystemObject");
        Assert.Contains(violations, v => v.Reason.Contains("shell/filesystem automation object"));
    }

    [Fact]
    public void Inspect_BlocksControlCharactersInArguments()
    {
        var line = new ScriptLine(1, "x", ["docs", "use", "na\u0007me"]);
        Assert.Contains(ScriptSafety.Inspect([line]), v => v.Reason.Contains("control character"));
    }

    [Theory]
    [InlineData(@"doc save --as ""C:\out\..\..\Windows\System32\x.cdr""", "traversal")]
    [InlineData(@"macro install --name M --file ""..\evil.bas""", "traversal")]
    public void Inspect_BlocksTraversalInPathArguments(string line, string expected)
        => Assert.Contains(Inspect(line), v => v.Reason.Contains(expected));

    [Fact]
    public void Inspect_AllowsAnAbsolutePathWithNoTraversalSegment()
        => Assert.Empty(Inspect(@"doc save --as ""C:\Users\someone\My Documents\out.cdr"""));

    [Theory]
    [InlineData("Sub Go()\n  Shell \"cmd.exe /c del *.*\"\nEnd Sub\n", "Shell")]
    [InlineData("Sub Go()\n  Set w = CreateObject(\"WScript.Shell\")\nEnd Sub\n", "CreateObject")]
    [InlineData("Sub Go()\n  Kill \"C:\\x.txt\"\nEnd Sub\n", "Kill")]
    [InlineData("Declare PtrSafe Function X Lib \"kernel32\" () As Long\n", "Declare")]
    [InlineData("Sub Go()\n  SaveSetting \"a\", \"b\", \"c\", \"d\"\nEnd Sub\n", "SaveSetting")]
    public void Inspect_BlocksDenylistedConstructsInInjectedVbaSource(string source, string expected)
    {
        using var bas = new TempScript(source, ".bas");
        IReadOnlyList<SafetyViolation> violations = Inspect($"macro install --name M --file \"{bas.Path}\"");
        Assert.Contains(violations, v => v.Reason.Contains(expected));
    }

    [Fact]
    public void Inspect_AllowsHarmlessVbaSource()
    {
        using var bas = new TempScript("Sub Regenerate()\n  ActiveDocument.ActivePage.SetSize 12, 19\nEnd Sub\n", ".bas");
        Assert.Empty(Inspect($"macro install --name M --file \"{bas.Path}\""));
    }

    [Fact]
    public void PathProblem_FlagsEmptyAndControlCharacterPaths()
    {
        Assert.Equal("empty path", ScriptSafety.PathProblem(""));
        Assert.Contains("control character", ScriptSafety.PathProblem("a\u0001b"));
        Assert.Null(ScriptSafety.PathProblem(@"C:\ok\file.cdr"));
    }
}
