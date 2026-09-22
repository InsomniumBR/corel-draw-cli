using System.Globalization;
using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>
/// `corel raw get/set/call <dotted.path> [args...]` - the generic escape hatch that
/// reaches the entire object model without a curated verb, via ComInvoker's dotted-path
/// walker.
/// </summary>
/// <remarks>`raw get` can invoke methods, not just read properties - e.g. `raw get X.Close` really closes X.</remarks>
public static class RawCommands
{
    private const string ActionWarning =
        "Note: `raw get` can invoke methods, not just read properties - e.g. `raw get X.Close` really closes X.";

    private const string SchemaHint =
        "If a member name or argument count looks wrong, run `corel schema generate` and check the cached " +
        "manifest (%LOCALAPPDATA%\\corel-draw-cli\\schemas\\corel-<version>.json) to confirm what actually exists.";

    public static int Run(string[] args, object app)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: corel raw <get|set|call> <path> [args...]");
            Console.Error.WriteLine(ActionWarning);
            return 1;
        }

        string op = args[0];
        string path = args[1];
        string[] rest = args[2..];

        try
        {
            switch (op)
            {
                case "get":
                    Console.WriteLine(FormatValue(ComInvoker.ResolvePath(app, path)));
                    return 0;

                case "set":
                    if (rest.Length < 1)
                    {
                        Console.Error.WriteLine("Usage: corel raw set <path> <value>");
                        return 1;
                    }
                    (object setTarget, string setMember) = ComInvoker.ResolveParent(app, path);
                    ComInvoker.Set(setTarget, setMember, ParseValue(rest[0])!);
                    Console.WriteLine("OK");
                    return 0;

                case "call":
                    (object callTarget, string callMember) = ComInvoker.ResolveParent(app, path);
                    object?[] callArgs = [.. rest.Select(ParseValue)];
                    object? result = ComInvoker.Call(callTarget, callMember, callArgs);
                    Console.WriteLine(result is null ? "OK" : result.ToString());
                    return 0;

                default:
                    Console.Error.WriteLine($"Unknown raw operation '{op}'. Expected get|set|call.");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ErrorMessages.Friendly(ex)}");
            if (!ErrorMessages.IsNoActiveDocument(ex))
            {
                Console.Error.WriteLine(SchemaHint);
            }
            return 1;
        }
    }

    /// <summary>Appends a COM object's resolved interface name so a bare "System.__ComObject" isn't the only output.</summary>
    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string text = value.ToString() ?? string.Empty;
        string? typeName = ComTypeName.TryResolve(value);
        return typeName is not null ? $"{text} ({typeName})" : text;
    }

    /// <summary>The literal string "null" maps to Type.Missing via ComInvoker's null convention.</summary>
    internal static object? ParseValue(string raw)
    {
        if (raw == "null")
        {
            return null;
        }
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
        {
            return d;
        }
        if (bool.TryParse(raw, out bool b))
        {
            return b;
        }
        return raw;
    }
}
