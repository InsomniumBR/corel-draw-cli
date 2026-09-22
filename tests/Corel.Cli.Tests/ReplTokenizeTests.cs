namespace Corel.Cli.Tests;

public class ReplTokenizeTests
{
    [Fact]
    public void Tokenize_SplitsOnWhitespace()
    {
        Assert.Equal(["shape", "list"], Repl.Tokenize("shape list"));
    }

    [Fact]
    public void Tokenize_StripsQuotesAndKeepsSpacesInside()
    {
        // This is the exact bug found while generating real layout files: a quoted
        // path in a .cdx script was passed through WITH its literal quote characters
        // still attached, so CorelDRAW silently failed to save against a malformed
        // path like `"C:\...\file.cdr"` (quotes included).
        string[] tokens = Repl.Tokenize("doc save --as \"C:\\Users\\wagne\\My Documents\\file.cdr\"");
        Assert.Equal(["doc", "save", "--as", @"C:\Users\wagne\My Documents\file.cdr"], tokens);
    }

    [Fact]
    public void Tokenize_HandlesMultipleQuotedArguments()
    {
        string[] tokens = Repl.Tokenize("macro install --name \"My Module\" --file \"C:\\some path\\x.bas\"");
        Assert.Equal(["macro", "install", "--name", "My Module", "--file", @"C:\some path\x.bas"], tokens);
    }

    [Fact]
    public void Tokenize_CollapsesRepeatedWhitespace()
    {
        Assert.Equal(["a", "b"], Repl.Tokenize("a    b"));
    }

    [Fact]
    public void Tokenize_EmptyLineProducesNoTokens()
    {
        Assert.Empty(Repl.Tokenize(""));
        Assert.Empty(Repl.Tokenize("   "));
    }

    [Fact]
    public void Tokenize_RejectsAnUnterminatedQuote()
    {
        Assert.False(Repl.TryTokenize("doc save --as \"C:\\out\\file.cdr", out _, out string? error));
        Assert.Contains("unterminated quote", error);
        Assert.Throws<FormatException>(() => Repl.Tokenize("\"still open"));
    }

    [Fact]
    public void Tokenize_DoubledQuoteInsideQuotesIsALiteralQuote()
        => Assert.Equal(["docs", "use", "a\"b"], Repl.Tokenize("docs use \"a\"\"b\""));

    [Fact]
    public void Tokenize_DoubledQuoteAloneIsAnEmptyToken()
        => Assert.Equal(["docs", "use", ""], Repl.Tokenize("docs use \"\""));
}
