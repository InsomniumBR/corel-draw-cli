using System.Globalization;

namespace Corel.Cli;

/// <summary>Minimal `--flag value` / positional argument splitter shared by every command.</summary>
public sealed class ArgReader
{
    private readonly Dictionary<string, string> _flags = [];
    private readonly List<string> _positional = [];

    public ArgReader(IEnumerable<string> args)
    {
        List<string> list = [.. args];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].StartsWith("--", StringComparison.Ordinal))
            {
                string name = list[i][2..];
                bool nextIsValue = i + 1 < list.Count && !list[i + 1].StartsWith("--", StringComparison.Ordinal);
                string value = nextIsValue ? list[++i] : "";
                _flags[name] = value;
            }
            else
            {
                _positional.Add(list[i]);
            }
        }
    }

    public IReadOnlyList<string> Positional => _positional;

    public string? Flag(string name) => _flags.GetValueOrDefault(name);

    public double? DoubleOrNull(string name)
    {
        string? s = Flag(name);
        return string.IsNullOrEmpty(s) ? null : double.Parse(s, CultureInfo.InvariantCulture);
    }
}
