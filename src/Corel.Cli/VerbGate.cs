using Corel.Automation;
using Corel.Schema;
using Corel.Verbs;

namespace Corel.Cli;

/// <summary>
/// Checks a curated command against the active CorelDRAW version's cached capability
/// manifest before running it - warns and proceeds rather than crashing cryptically when
/// the verb isn't in that version's object model. Never blocks: a missing manifest (no
/// `corel schema generate` run yet) or unknown verb just skips the check silently.
/// </summary>
public static class VerbGate
{
    private static readonly VerbRegistry Registry = VerbRegistry.LoadEmbedded();

    // Cached per process + version: the manifest is a ~1MB JSON file, and this gate
    // fires on every curated command - re-reading it from disk on each call added real,
    // avoidable latency between COM calls in a script/REPL session (many calls in a
    // row), which is worth eliminating regardless of any other effect.
    private static readonly Dictionary<string, CapabilityManifest?> ManifestCache = [];

    public static void WarnIfUnavailable(string verbName, string? corelVersion)
    {
        VerbDefinition? verb = Registry.Find(verbName);
        CapabilityManifest? manifest = GetManifest(corelVersion);
        if (verb is null || manifest is null)
        {
            return;
        }

        AvailabilityResult result = VerbAvailability.IsAvailable(verb, manifest);
        if (result.Status == AvailabilityStatus.Unavailable)
        {
            Console.Error.WriteLine($"Warning: {result.Reason} Attempting anyway.");
        }
    }

    /// <summary>Loads (and caches) the active version's manifest, shared with other manifest-backed lookups.</summary>
    public static CapabilityManifest? GetManifest(string? corelVersion)
    {
        if (corelVersion is null)
        {
            return null;
        }

        if (!ManifestCache.TryGetValue(corelVersion, out CapabilityManifest? manifest))
        {
            manifest = InstallStore.LoadManifest(corelVersion);
            ManifestCache[corelVersion] = manifest;
        }
        return manifest;
    }
}
