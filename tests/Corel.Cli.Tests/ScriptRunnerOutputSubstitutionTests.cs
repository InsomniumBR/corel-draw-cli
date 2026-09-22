using Corel.Cli;

namespace Corel.Cli.Tests;

public class ScriptRunnerOutputSubstitutionTests
{
    [Fact]
    public void ValidateOutputSubstitution_ReturnsNull_WhenNoPlaceholderUsed()
    {
        string?[] lines = ["doc new", "shape list"];
        Assert.Null(ScriptRunner.ValidateOutputSubstitution(lines!, output: null));
    }

    [Fact]
    public void ValidateOutputSubstitution_ReturnsError_WhenPlaceholderUsedWithoutOutput()
    {
        string[] lines = ["doc new", "doc save --as \"${OUTPUT}\""];
        string? error = ScriptRunner.ValidateOutputSubstitution(lines, output: null);
        Assert.NotNull(error);
        Assert.Contains("${OUTPUT}", error);
    }

    [Fact]
    public void ValidateOutputSubstitution_ReturnsNull_WhenPlaceholderUsedWithOutput()
    {
        string[] lines = ["doc save --as \"${OUTPUT}\""];
        Assert.Null(ScriptRunner.ValidateOutputSubstitution(lines, output: @"C:\out.cdr"));
    }

    [Fact]
    public void ApplyOutputSubstitution_ReplacesPlaceholder()
    {
        string[] lines = ["doc new", "doc save --as \"${OUTPUT}\""];
        string[] result = [.. ScriptRunner.ApplyOutputSubstitution(lines, @"C:\out.cdr")];
        Assert.Equal("doc new", result[0]);
        Assert.Equal("doc save --as \"C:\\out.cdr\"", result[1]);
    }

    [Fact]
    public void ApplyOutputSubstitution_LeavesLinesUnchanged_WhenOutputIsNull()
    {
        string[] lines = ["doc save --as \"${OUTPUT}\""];
        string[] result = [.. ScriptRunner.ApplyOutputSubstitution(lines, null)];
        Assert.Equal(lines, result);
    }
}
