namespace Corel.Cli.Tests;

public class ErrorMessagesTests
{
    [Fact]
    public void Friendly_ReturnsInnerExceptionMessage_WhenPresent()
    {
        var inner = new InvalidOperationException("real cause");
        var outer = new Exception("wrapper", inner);
        Assert.Equal("real cause", ErrorMessages.Friendly(outer));
    }

    [Fact]
    public void Friendly_ReturnsOwnMessage_WhenNoInnerException()
    {
        var ex = new InvalidOperationException("standalone");
        Assert.Equal("standalone", ErrorMessages.Friendly(ex));
    }

    [Fact]
    public void Friendly_ReturnsRemedyHint_ForNullActiveDocumentCase()
    {
        var ex = new InvalidOperationException(
            "'ActiveDocument' resolved to null while walking path 'ActiveDocument'.");

        string message = ErrorMessages.Friendly(ex);

        Assert.Contains("corel doc new", message);
        Assert.DoesNotContain("walking path", message);
    }

    [Fact]
    public void Friendly_ReturnsRemedyHint_ForNullActiveDocumentCase_WrappedInInnerException()
    {
        var inner = new InvalidOperationException(
            "'ActiveDocument' resolved to null while walking path 'ActiveDocument.ActivePage'.");
        var outer = new Exception("wrapper", inner);

        Assert.Contains("corel doc new", ErrorMessages.Friendly(outer));
    }

    [Fact]
    public void IsNoActiveDocument_TrueOnlyForThatSpecificMessage()
    {
        var noDoc = new InvalidOperationException(
            "'ActiveDocument' resolved to null while walking path 'ActiveDocument'.");
        var somethingElse = new InvalidOperationException("'ActiveLayer' resolved to null while walking path 'x'.");

        Assert.True(ErrorMessages.IsNoActiveDocument(noDoc));
        Assert.False(ErrorMessages.IsNoActiveDocument(somethingElse));
    }
}
