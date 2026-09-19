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

    public static object? Get(object target, string name, params object?[] args)
        => target.GetType().InvokeMember(
            name, BindingFlags.GetProperty, null, target, NormalizeArgs(args));

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
    public static object? ResolvePath(object root, string dottedPath)
    {
        object current = root;
        foreach (string segment in SplitPath(dottedPath))
        {
            current = Get(current, segment)
                ?? throw new InvalidOperationException(
                    $"'{segment}' resolved to null while walking path '{dottedPath}'.");
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
