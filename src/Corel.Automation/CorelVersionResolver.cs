using Corel.Schema;

namespace Corel.Automation;

/// <summary>
/// Resolves --corel-version (or its absence) against the local discovery record.
/// </summary>
public static class CorelVersionResolver
{
    public static CorelInstall Resolve(InstallRecord record, string? requestedVersion)
    {
        if (record.Installs.Count == 0)
        {
            throw new InvalidOperationException(
                "No CorelDRAW installs are recorded. Run `corel detect` first.");
        }

        string version = requestedVersion ?? record.DefaultVersion
            ?? throw new InvalidOperationException(
                "No default CorelDRAW version is set and none was specified with --corel-version.");

        return record.Installs.Find(i => i.Version == version)
            ?? throw new InvalidOperationException(
                $"CorelDRAW version '{version}' was not found in the discovery record. " +
                $"Known versions: {string.Join(", ", record.Installs.ConvertAll(i => i.Version))}.");
    }
}
