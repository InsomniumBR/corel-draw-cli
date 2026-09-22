using Corel.Automation;
using Corel.Schema;
using Corel.Verbs;

namespace Corel.Cli;

/// <summary>The outcome of preflighting a .cdx script. <see cref="BlockingErrors"/> can never be
/// overridden; <see cref="OverridableErrors"/> are the findings <c>--force</c> waives.</summary>
public sealed record ScriptPreparation(
    IReadOnlyList<ScriptLine> Lines,
    IReadOnlyList<string> BlockingErrors,
    IReadOnlyList<string> OverridableErrors,
    bool MissingVariables);

/// <summary>Runs a .cdx script - plain text, one command per line, identical grammar to the REPL
/// minus the `corel` prefix, `#` for comments. AGENTS.md lists the full preflight contract.</summary>
public static class ScriptRunner
{
    public static int Run(string path, CorelSession session, IReadOnlyDictionary<string, string> vars, bool force)
    {
        ScriptPreparation preparation = Prepare(path, vars, session.Version);

        if (preparation.BlockingErrors.Count > 0)
        {
            foreach (string error in preparation.BlockingErrors)
            {
                if (preparation.MissingVariables)
                {
                    ConsoleWriter.WriteWarning(error);
                }
                else
                {
                    Console.Error.WriteLine(error);
                }
            }
            return 1;
        }

        if (preparation.OverridableErrors.Count > 0)
        {
            Console.Error.WriteLine($"Preflight failed: {preparation.OverridableErrors.Count} problem(s) found:");
            foreach (string error in preparation.OverridableErrors)
            {
                Console.Error.WriteLine($"  {error}");
            }
            if (!force)
            {
                Console.Error.WriteLine("Aborting without running anything. Pass --force to run anyway.");
                return 1;
            }
            Console.Error.WriteLine("--force given: running anyway.");
        }

        return Execute(preparation.Lines, session);
    }

    public static ScriptPreparation Prepare(string path, IReadOnlyDictionary<string, string> vars, string? corelVersion)
    {
        if (!File.Exists(path))
        {
            return new ScriptPreparation([], [$"Script file not found: {path}"], [], MissingVariables: false);
        }

        List<(int Number, string Text)> rawLines = [.. File
            .ReadLines(path)
            .Select((text, index) => (Number: index + 1, Text: text.Trim()))
            .Where(l => l.Text.Length > 0 && !l.Text.StartsWith('#'))];

        if (ScriptVars.Validate(rawLines.Select(l => l.Text), vars) is string varsError)
        {
            return new ScriptPreparation([], [varsError], [], MissingVariables: true);
        }

        List<ScriptLine> lines = [];
        List<string> blocking = [];
        foreach ((int number, string text) in rawLines)
        {
            string substituted = ScriptVars.Apply([text], vars).Single();
            if (!Repl.TryTokenize(substituted, out string[] tokens, out string? tokenError))
            {
                blocking.Add($"line {number}: {tokenError}");
                continue;
            }

            lines.Add(new ScriptLine(number, substituted, tokens));

            if (CommandValidator.Validate(tokens) is string structuralError)
            {
                blocking.Add($"line {number}: {structuralError}");
            }
            else if (CommandValidator.ValidateResources(tokens) is string resourceError)
            {
                blocking.Add($"line {number}: {resourceError}");
            }
        }

        if (blocking.Count > 0)
        {
            return new ScriptPreparation(lines, blocking, [], MissingVariables: false);
        }

        List<string> overridable =
        [
            .. ScriptSafety.Inspect(lines).Select(v => $"line {v.LineNumber}: {v.Reason} ({v.Line})"),
            .. Preflight(lines, corelVersion).Select(p => $"line {p.Line.Number}: {p.Result.Reason}"),
        ];

        return new ScriptPreparation(lines, [], overridable, MissingVariables: false);
    }

    private static int Execute(IReadOnlyList<ScriptLine> lines, CorelSession session)
    {
        foreach (ScriptLine line in lines)
        {
            Console.WriteLine($"$ {line.Text}");
            try
            {
                int exitCode = CommandDispatcher.Dispatch(line.Tokens, session);
                if (exitCode != 0)
                {
                    Console.Error.WriteLine($"Command exited with code {exitCode}; stopping script.");
                    return exitCode;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ErrorMessages.Friendly(ex)}");
                return 1;
            }
        }

        return 0;
    }

    private static List<(ScriptLine Line, AvailabilityResult Result)> Preflight(
        IReadOnlyList<ScriptLine> lines, string? corelVersion)
    {
        var incompatible = new List<(ScriptLine, AvailabilityResult)>();
        if (corelVersion is null)
        {
            return incompatible;
        }

        CapabilityManifest? manifest = InstallStore.LoadManifest(corelVersion);
        if (manifest is null)
        {
            return incompatible;
        }

        VerbRegistry registry = VerbRegistry.LoadEmbedded();
        foreach (ScriptLine line in lines)
        {
            if (ResolveVerbName(line.Tokens) is not string verbName)
            {
                continue;
            }

            VerbDefinition? verb = registry.Find(verbName);
            if (verb is null)
            {
                continue;
            }

            AvailabilityResult result = VerbAvailability.IsAvailable(verb, manifest);
            if (result.Status == AvailabilityStatus.Unavailable)
            {
                incompatible.Add((line, result));
            }
        }

        return incompatible;
    }

    /// <summary>Maps a tokenized line to its verbs.json entry for the availability check; null
    /// means "no single registry entry to check", not "not validated".</summary>
    internal static string? ResolveVerbName(string[] tokens)
    {
        if (tokens.Length < 2)
        {
            return null;
        }

        return (tokens[0], tokens[1]) switch
        {
            ("page", "size") when tokens.Any(t => t == "--width") => "page.size.set",
            ("page", "size") => "page.size.get",
            ("shape", "create-rect") => "shape.create-rect",
            ("shape", "delete") => "shape.delete",
            ("doc", "save") => "doc.save",
            ("macro", "install") => "macro.install",
            _ => null,
        };
    }
}
