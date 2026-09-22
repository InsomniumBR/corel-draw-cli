using System.Text.RegularExpressions;

namespace Corel.Cli;

/// <summary>
/// Generic key/value substitution for .cdx scripts: `--vars KEY=VALUE,KEY2=VALUE2`
/// on the command line, `{KEY}` anywhere in the script. Deliberately generic rather
/// than a single hardcoded output-path token - a script can parameterize anything
/// (an output path, a page size, a unit, ...), not just where it saves.
/// </summary>
public static partial class ScriptVars
{
    [GeneratedRegex(@"\{([A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex PlaceholderPattern();

    /// <summary>
    /// Parses `KEY=VALUE,KEY2=VALUE2`. A value may itself contain `=` (split only on
    /// the first one); a value may not contain a literal comma (use a second --vars
    /// entry, or avoid commas in the value, if that's ever needed).
    /// </summary>
    public static Dictionary<string, string> Parse(string? raw)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(raw))
        {
            return result;
        }

        foreach (string pair in raw.Split(','))
        {
            int eq = pair.IndexOf('=');
            if (eq < 0)
            {
                continue;
            }
            string key = pair[..eq].Trim();
            string value = pair[(eq + 1)..];
            if (key.Length > 0)
            {
                result[key] = value;
            }
        }
        return result;
    }

    /// <summary>
    /// Returns an error message listing every `{PLACEHOLDER}` referenced in the script
    /// that has no matching entry in <paramref name="vars"/>, or null if every
    /// placeholder used is covered.
    /// </summary>
    public static string? Validate(IEnumerable<string> rawLines, IReadOnlyDictionary<string, string> vars)
    {
        SortedSet<string> missing = [];
        foreach (string line in rawLines)
        {
            foreach (Match match in PlaceholderPattern().Matches(line))
            {
                string key = match.Groups[1].Value;
                if (!vars.ContainsKey(key))
                {
                    missing.Add(key);
                }
            }
        }

        if (missing.Count == 0)
        {
            return null;
        }

        string placeholders = string.Join(", ", missing.Select(k => $"{{{k}}}"));
        return $"This script references undefined variable(s): {placeholders}. " +
               "Pass them via --vars KEY=VALUE,KEY2=VALUE2,...";
    }

    public static IEnumerable<string> Apply(IEnumerable<string> rawLines, IReadOnlyDictionary<string, string> vars)
        => rawLines.Select(line => PlaceholderPattern().Replace(
            line,
            m => vars.TryGetValue(m.Groups[1].Value, out string? value) ? value : m.Value));
}
