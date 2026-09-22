using System.Text.RegularExpressions;

namespace Corel.Cli;

public sealed record SafetyViolation(int LineNumber, string Line, string Reason);

/// <summary>Load-time screening of a .cdx script for operations outside drawing automation.
/// AGENTS.md documents exactly what is checked and what is out of scope.</summary>
public static partial class ScriptSafety
{
    // Matched whole-segment so a CorelDRAW member merely containing one of these
    // (ShellSortOrder) is unaffected.
    private static readonly HashSet<string> DeniedRawSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell", "ShellExecute", "Run", "RunMacro", "Exec", "Execute",
        "CreateObject", "GetObject", "WScript", "WshShell", "FileSystemObject",
        "Kill", "RmDir", "SetAttr",
        "RegWrite", "RegRead", "RegDelete", "SaveSetting", "GetSetting", "DeleteSetting",
        "VBE", "VBProjects", "VBComponents", "CodeModule", "AddFromString", "AddFromFile",
    };

    private static readonly string[] DeniedProgIds =
    [
        "WScript.Shell", "Shell.Application", "Scripting.FileSystemObject",
        "MSXML2.XMLHTTP", "InternetExplorer.Application",
    ];

    [GeneratedRegex(@"\b(Shell|CreateObject|GetObject|Kill|RmDir|SetAttr|RegWrite|RegRead|RegDelete|SaveSetting|GetSetting|DeleteSetting|Declare)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex DangerousVbaPattern();

    private static readonly string[] PathFlags = ["--as", "--file"];

    public static IReadOnlyList<SafetyViolation> Inspect(IReadOnlyList<ScriptLine> lines)
    {
        var violations = new List<SafetyViolation>();
        foreach (ScriptLine line in lines)
        {
            InspectTokens(line, violations);
            InspectRaw(line, violations);
            InspectMacroSource(line, violations);
        }
        return violations;
    }

    private static void InspectTokens(ScriptLine line, List<SafetyViolation> violations)
    {
        foreach (string token in line.Tokens)
        {
            if (token.Any(char.IsControl))
            {
                violations.Add(new SafetyViolation(line.Number, line.Text,
                    "argument contains a control character"));
            }
            if (DeniedProgIds.Any(p => string.Equals(token, p, StringComparison.OrdinalIgnoreCase)))
            {
                violations.Add(new SafetyViolation(line.Number, line.Text,
                    $"argument '{token}' creates a shell/filesystem automation object"));
            }
        }

        for (int i = 0; i < line.Tokens.Length - 1; i++)
        {
            if (!PathFlags.Contains(line.Tokens[i], StringComparer.Ordinal))
            {
                continue;
            }
            if (PathProblem(line.Tokens[i + 1]) is string problem)
            {
                violations.Add(new SafetyViolation(line.Number, line.Text, $"{line.Tokens[i]}: {problem}"));
            }
        }
    }

    private static void InspectRaw(ScriptLine line, List<SafetyViolation> violations)
    {
        if (line.Tokens is not ["raw", "get" or "set" or "call", string path, ..])
        {
            return;
        }

        foreach (string segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (DeniedRawSegments.Contains(segment))
            {
                violations.Add(new SafetyViolation(line.Number, line.Text,
                    $"raw path reaches denylisted member '{segment}'"));
            }
        }
    }

    private static void InspectMacroSource(ScriptLine line, List<SafetyViolation> violations)
    {
        if (line.Tokens is not ["macro", "install", ..])
        {
            return;
        }

        string? file = CommandValidator.ReadFlagValue(line.Tokens[2..], "file");
        if (file is null || !File.Exists(file))
        {
            return;
        }

        var hits = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in DangerousVbaPattern().Matches(File.ReadAllText(file)))
        {
            hits.Add(match.Value);
        }
        if (hits.Count > 0)
        {
            violations.Add(new SafetyViolation(line.Number, line.Text,
                $"VBA source '{file}' uses denylisted construct(s): {string.Join(", ", hits)}"));
        }
    }

    internal static string? PathProblem(string path)
    {
        if (path.Length == 0)
        {
            return "empty path";
        }
        if (path.Any(char.IsControl))
        {
            return "path contains a control character";
        }
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return "path contains characters the OS rejects in a path";
        }

        string[] segments = path.Split('/', '\\');
        return segments.Contains("..", StringComparer.Ordinal)
            ? "path contains a '..' traversal segment - pass an already-resolved absolute path instead"
            : null;
    }
}
