using Corel.Automation;
using Corel.Schema;
using Corel.Verbs;

namespace Corel.Cli;

/// <summary>
/// Runs a .cdx script - plain text, one command per line, identical grammar to the REPL
/// minus the `corel` prefix, `#` for comments. Preflights every line against the active
/// CorelDRAW version's cached capability manifest before executing anything, so an
/// incompatible script fails loudly up front rather than partway through a run that's
/// already made changes.
/// </summary>
public static class ScriptRunner
{
    private const string OutputPlaceholder = "${OUTPUT}";

    public static int Run(string path, object app, string? corelVersion, string? output, bool force)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Script file not found: {path}");
            return 1;
        }

        List<string> rawLines = [.. File
            .ReadLines(path)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))];

        string? substitutionError = ValidateOutputSubstitution(rawLines, output);
        if (substitutionError is not null)
        {
            Console.Error.WriteLine(substitutionError);
            return 1;
        }

        List<string[]> lines = [.. ApplyOutputSubstitution(rawLines, output).Select(Repl.Tokenize)];

        List<(string[] Tokens, AvailabilityResult Result)> incompatible = Preflight(lines, corelVersion);
        if (incompatible.Count > 0)
        {
            Console.Error.WriteLine($"Preflight failed: {incompatible.Count} command(s) may not be available in CorelDRAW {corelVersion}:");
            foreach ((string[] tokens, AvailabilityResult result) in incompatible)
            {
                Console.Error.WriteLine($"  '{string.Join(' ', tokens)}': {result.Reason}");
            }

            if (!force)
            {
                Console.Error.WriteLine("Aborting without running anything. Pass --force to run anyway.");
                return 1;
            }

            Console.Error.WriteLine("--force given: running anyway.");
        }

        foreach (string[] tokens in lines)
        {
            Console.WriteLine($"$ {string.Join(' ', tokens)}");
            try
            {
                int exitCode = CommandDispatcher.Dispatch(tokens, () => (app, corelVersion));
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

    /// <summary>
    /// Returns an error message if the script references ${OUTPUT} without --output
    /// having been given, or null if it's fine to proceed.
    /// </summary>
    internal static string? ValidateOutputSubstitution(IEnumerable<string> rawLines, string? output)
    {
        bool usesPlaceholder = rawLines.Any(l => l.Contains(OutputPlaceholder, StringComparison.Ordinal));
        if (usesPlaceholder && output is null)
        {
            return $"This script uses {OutputPlaceholder} but no --output <path> was given. " +
                   "Pass --output, or edit the script to hardcode a path.";
        }
        return null;
    }

    internal static IEnumerable<string> ApplyOutputSubstitution(IEnumerable<string> rawLines, string? output)
        => output is null
            ? rawLines
            : rawLines.Select(l => l.Replace(OutputPlaceholder, output, StringComparison.Ordinal));

    private static List<(string[] Tokens, AvailabilityResult Result)> Preflight(List<string[]> lines, string? corelVersion)
    {
        var incompatible = new List<(string[], AvailabilityResult)>();
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
        foreach (string[] tokens in lines)
        {
            string? verbName = ResolveVerbName(tokens);
            if (verbName is null)
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
                incompatible.Add((tokens, result));
            }
        }

        return incompatible;
    }

    /// <summary>
    /// Mirrors CommandDispatcher's grammar to find which verbs.json entry (if any) a
    /// tokenized line corresponds to. Commands with no single verbs.json entry (raw,
    /// detect, schema, docs use/list/active) resolve to null - not availability-checked.
    /// </summary>
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
