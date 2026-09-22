using System.Reflection;

namespace Corel.Automation;

/// <summary>
/// Late-bound COM member access via Type.InvokeMember (classic IDispatch reflection).
/// </summary>
/// <remarks>
/// C#'s `dynamic`/DLR COM binder was found empirically unreliable against CorelDRAW's
/// object model - it failed to bind a real method that Type.InvokeMember handles
/// correctly. Every COM call in this project goes through here instead.
/// </remarks>
public static class ComInvoker
{
    /// <summary>
    /// A caller passes null for any argument it wants treated as "omitted" - this maps
    /// to Type.Missing, the standard .NET interop marker for a skipped optional COM
    /// parameter (e.g. Layer.CreateRectangle's four optional corner-rounding params).
    /// </summary>
    private static object?[] NormalizeArgs(object?[] args)
        => Array.ConvertAll(args, a => a ?? Type.Missing);

    /// <summary>
    /// Combines GetProperty|InvokeMethod because not every COM library agrees on which
    /// one an indexed accessor is: CorelDRAW's own collections expose "Item" as a true
    /// dispinterface property, but VBIDE's (VBProjects, VBComponents, etc.) expose it as
    /// a method - GetProperty alone throws DISP_E_MEMBERNOTFOUND on the latter.
    /// </summary>
    public static object? Get(object target, string name, params object?[] args)
        => target.GetType().InvokeMember(
            name, BindingFlags.GetProperty | BindingFlags.InvokeMethod, null, target, NormalizeArgs(args));

    public static void Set(object target, string name, object value)
        => target.GetType().InvokeMember(
            name, BindingFlags.SetProperty, null, target, [value]);

    public static object? Call(object target, string name, params object?[] args)
        => target.GetType().InvokeMember(
            name, BindingFlags.InvokeMethod, null, target, NormalizeArgs(args));

    /// <summary>
    /// Walks a dotted path (e.g. "ActiveDocument.ActivePage.SizeWidth") from a root COM
    /// object, resolving every segment as a property get, and returns the final value.
    /// </summary>
    /// <remarks>
    /// A null on an intermediate segment is a genuine walking failure - there's nothing
    /// to resolve the next segment against. A null on the LAST segment is ambiguous:
    /// Get() combines GetProperty|InvokeMethod, so the final segment may have been a
    /// method call that ran successfully and simply has no return value (e.g. Close()) -
    /// that's not a failure, so it gets a different message rather than being reported
    /// as one.
    /// </remarks>
    public static object? ResolvePath(object root, string dottedPath)
    {
        string[] segments = SplitPath(dottedPath);
        object current = root;
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            object? next = Get(current, segment);
            if (next is null)
            {
                bool isLastSegment = i == segments.Length - 1;
                throw new InvalidOperationException(isLastSegment
                    ? $"'{segment}' was invoked and returned no value (it may have performed an action, e.g. closing/quitting)."
                    : $"'{segment}' resolved to null while walking path '{dottedPath}'.");
            }
            current = next;
        }
        return current;
    }

    /// <summary>
    /// Walks all but the last segment of a dotted path and returns the resolved parent
    /// object plus the final member name - for `raw set`/`raw call`, which need to act
    /// on the parent rather than just read a value.
    /// </summary>
    public static (object Target, string Member) ResolveParent(object root, string dottedPath)
    {
        string[] segments = SplitPath(dottedPath);
        if (segments.Length == 0)
        {
            throw new ArgumentException("Path must have at least one segment.", nameof(dottedPath));
        }

        object current = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            current = Get(current, segments[i])
                ?? throw new InvalidOperationException(
                    $"'{segments[i]}' resolved to null while walking path '{dottedPath}'.");
        }
        return (current, segments[^1]);
    }

    private static string[] SplitPath(string dottedPath)
        => dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
