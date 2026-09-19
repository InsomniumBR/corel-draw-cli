namespace Corel.Cli;

/// <summary>
/// Type.InvokeMember wraps COM failures in a TargetInvocationException whose own
/// Message is a useless "Exception has been thrown by the target of an invocation." -
/// the real, useful message is always on InnerException.
/// </summary>
public static class ErrorMessages
{
    public static string Friendly(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
