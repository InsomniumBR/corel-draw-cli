namespace Corel.Cli;

/// <summary>
/// Type.InvokeMember wraps COM failures in a TargetInvocationException whose own
/// Message is a useless "Exception has been thrown by the target of an invocation." -
/// the real, useful message is always on InnerException.
/// </summary>
public static class ErrorMessages
{
    private const string NoActiveDocumentHint =
        "No document is open. Run `corel doc new` to create one, or open a document in CorelDRAW first.";

    public static string Friendly(Exception ex)
    {
        string message = RawMessage(ex);
        return IsNoActiveDocument(ex) ? NoActiveDocumentHint : message;
    }

    /// <summary>True for ComInvoker.ResolvePath's null-ActiveDocument case, so callers can skip an unrelated hint for it.</summary>
    public static bool IsNoActiveDocument(Exception ex)
        => RawMessage(ex).StartsWith("'ActiveDocument' resolved to null", StringComparison.Ordinal);

    private static string RawMessage(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
