using Corel.Schema;

namespace Corel.Verbs;

public enum AvailabilityStatus
{
    /// <summary>The verb declares no requirement, or the manifest confirms the member exists.</summary>
    Available,

    /// <summary>The required interface or member is missing from the active manifest.</summary>
    Unavailable,
}

public sealed record AvailabilityResult(AvailabilityStatus Status, string? Reason);

public static class VerbAvailability
{
    public static AvailabilityResult IsAvailable(VerbDefinition verb, CapabilityManifest manifest)
    {
        if (verb.RequiresInterface is null)
        {
            return new AvailabilityResult(AvailabilityStatus.Available, null);
        }

        if (!manifest.Interfaces.TryGetValue(verb.RequiresInterface, out InterfaceInfo? iface))
        {
            return new AvailabilityResult(
                AvailabilityStatus.Unavailable,
                $"Interface '{verb.RequiresInterface}' is not present in the CorelDRAW {manifest.CorelVersion} object model.");
        }

        if (verb.RequiresMember is not null && !iface.Members.ContainsKey(verb.RequiresMember))
        {
            return new AvailabilityResult(
                AvailabilityStatus.Unavailable,
                $"'{verb.RequiresInterface}.{verb.RequiresMember}' is not present in the CorelDRAW {manifest.CorelVersion} object model.");
        }

        return new AvailabilityResult(AvailabilityStatus.Available, null);
    }
}
