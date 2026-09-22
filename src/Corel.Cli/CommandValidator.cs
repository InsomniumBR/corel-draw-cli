using System.Globalization;
using Corel.Automation;

namespace Corel.Cli;

public sealed record ParsedArgs(
    IReadOnlyDictionary<string, string> Values,
    IReadOnlySet<string> Booleans,
    IReadOnlyList<string> Positional)
{
    public string? Value(string name) => Values.GetValueOrDefault(name);

    public bool Has(string name) => Booleans.Contains(name);
}

/// <summary>Structural validation of one tokenized command line against <see cref="CommandGrammar"/>,
/// run before any COM connection and - for scripts - before line 1 executes.</summary>
/// <remarks>`raw` is checked only against its own fixed grammar: its path and call arguments are
/// arbitrary object-model members, which only the capability manifest can confirm.</remarks>
public static class CommandValidator
{
    public static string? Validate(string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return "Empty command.";
        }

        string group = tokens[0];
        if (!CommandGrammar.IsGroup(group))
        {
            return $"Unknown command '{group}'. Run `corel help` to see every command.";
        }

        IReadOnlyList<string> subs = CommandGrammar.SubcommandsOf(group);
        if (subs.Count > 0)
        {
            if (tokens.Length < 2)
            {
                return $"Command '{group}' needs a subcommand. Expected: {string.Join(", ", subs)}.";
            }
            if (!subs.Contains(tokens[1], StringComparer.Ordinal))
            {
                return $"Unknown {group} subcommand '{tokens[1]}'. Expected: {string.Join(", ", subs)}.";
            }
        }

        CommandSpec spec = CommandGrammar.Find(tokens)!;
        return ValidateArgs(spec, tokens[spec.Words..]);
    }

    /// <summary>Preconditions a structural pass cannot see; separate from <see cref="Validate"/>
    /// because it touches the filesystem.</summary>
    public static string? ValidateResources(string[] tokens)
    {
        if (tokens is not ["macro", "install", ..])
        {
            return null;
        }

        string? file = ReadFlagValue(tokens[2..], "file");
        return file is not null && !File.Exists(file)
            ? $"macro source file not found: {file}"
            : null;
    }

    private static string? ValidateArgs(CommandSpec spec, string[] args)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var positional = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            bool trailing = spec.ArbitraryTrailingArgs && positional.Count >= spec.MinPositional;
            if (!arg.StartsWith("--", StringComparison.Ordinal) || trailing)
            {
                positional.Add(arg);
                continue;
            }

            string name = arg[2..];
            FlagSpec? flag = spec.FindFlag(name);
            if (flag is null)
            {
                string known = spec.Flags.Count == 0
                    ? "it takes no flags"
                    : $"known flags: {string.Join(", ", spec.Flags.Select(f => "--" + f.Name))}";
                return Fail(spec, $"unknown flag '--{name}' ({known})");
            }
            if (!seen.Add(name))
            {
                return Fail(spec, $"flag '--{name}' given more than once");
            }
            if (flag.Kind == FlagKind.Boolean)
            {
                continue;
            }
            if (i + 1 >= args.Length)
            {
                return Fail(spec, $"flag '--{name}' needs a value");
            }

            string value = args[++i];
            if (value.StartsWith("--", StringComparison.Ordinal))
            {
                return Fail(spec, $"flag '--{name}' needs a value, but the next argument is another flag ('{value}')");
            }
            if (ValidateValue(flag, value) is string valueError)
            {
                return Fail(spec, valueError);
            }
        }

        foreach (FlagSpec required in spec.Flags.Where(f => f.Required))
        {
            if (!seen.Contains(required.Name))
            {
                return Fail(spec, $"missing required flag '--{required.Name}'");
            }
        }

        if (spec.RequiredTogether is { Count: > 0 } together)
        {
            int present = together.Count(seen.Contains);
            if (present > 0 && present < together.Count)
            {
                string all = string.Join(", ", together.Select(f => "--" + f));
                return Fail(spec, $"{all} must be given together");
            }
        }

        if (positional.Count < spec.MinPositional)
        {
            return Fail(spec, $"expected at least {spec.MinPositional} argument(s), got {positional.Count}");
        }
        if (positional.Count > spec.MaxPositional)
        {
            return Fail(spec, $"expected at most {spec.MaxPositional} argument(s), got {positional.Count}");
        }

        return null;
    }

    private static string? ValidateValue(FlagSpec flag, string value) => flag.Value switch
    {
        ValueKind.Number when !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            => $"'--{flag.Name} {value}' is not a number",
        ValueKind.Integer when !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            => $"'--{flag.Name} {value}' is not a whole number",
        ValueKind.Unit when !UnitNames.IsKnown(value)
            => $"unknown unit '{value}'. Known units: {string.Join(", ", UnitNames.Known)}",
        ValueKind.Path or ValueKind.Text when value.Length == 0
            => $"flag '--{flag.Name}' needs a non-empty value",
        _ => null,
    };

    private static string Fail(CommandSpec spec, string reason)
        => $"{spec.Name}: {reason}.{Environment.NewLine}Usage: {spec.Usage}";

    /// <summary>Splits an already-validated argument list using the command's own flag kinds,
    /// so a boolean flag never swallows the token after it.</summary>
    public static ParsedArgs Parse(CommandSpec spec, string[] args)
    {
        Dictionary<string, string> values = [];
        HashSet<string> booleans = [];
        List<string> positional = [];

        for (int i = 0; i < args.Length; i++)
        {
            bool trailing = spec.ArbitraryTrailingArgs && positional.Count >= spec.MinPositional;
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || trailing)
            {
                positional.Add(args[i]);
                continue;
            }

            string name = args[i][2..];
            if (spec.FindFlag(name) is { Kind: FlagKind.Boolean })
            {
                booleans.Add(name);
            }
            else if (i + 1 < args.Length)
            {
                values[name] = args[++i];
            }
        }

        return new ParsedArgs(values, booleans, positional);
    }

    internal static string? ReadFlagValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--" + name)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
