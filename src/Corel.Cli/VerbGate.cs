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

    public static void WarnIfUnavailable(string verbName, string? corelVersion)
    {
        VerbDefinition? verb = Registry.Find(verbName);
        if (verb is null || corelVersion is null)
        {
            return;
        }

        CapabilityManifest? manifest = InstallStore.LoadManifest(corelVersion);
        if (manifest is null)
        {
            return;
        }

        AvailabilityResult result = VerbAvailability.IsAvailable(verb, manifest);
        if (result.Status == AvailabilityStatus.Unavailable)
        {
            Console.Error.WriteLine($"Warning: {result.Reason} Attempting anyway.");
        }
    }
}
