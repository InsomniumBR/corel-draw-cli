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
                string value = i + 1 < list.Count ? list[++i] : "";
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

    public double Double(string name)
        => double.Parse(
            Flag(name) ?? throw new ArgumentException($"Missing required --{name}"),
            CultureInfo.InvariantCulture);

    public double? DoubleOrNull(string name)
        => Flag(name) is { } s ? double.Parse(s, CultureInfo.InvariantCulture) : null;
}
