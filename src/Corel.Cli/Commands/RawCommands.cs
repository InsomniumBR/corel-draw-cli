using System.Globalization;
using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>
/// `corel raw get/set/call <dotted.path> [args...]` - the generic escape hatch that
/// reaches the entire object model without a curated verb, via ComInvoker's dotted-path
/// walker.
/// </summary>
public static class RawCommands
{
    public static int Run(string[] args, object app)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: corel raw <get|set|call> <path> [args...]");
            return 1;
        }

        string op = args[0];
        string path = args[1];
        string[] rest = args[2..];

        switch (op)
        {
            case "get":
                Console.WriteLine(ComInvoker.ResolvePath(app, path));
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
