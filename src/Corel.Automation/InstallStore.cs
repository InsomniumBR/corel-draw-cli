using System.Runtime.Versioning;
using Corel.Schema;

namespace Corel.Automation;

/// <summary>
/// Reads/writes the local discovery record and per-version capability manifests under
/// %LOCALAPPDATA%\corel-draw-cli\.
/// </summary>
[SupportedOSPlatform("windows")]
public static class InstallStore
{
    private static string RootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "corel-draw-cli");

    public static string RecordPath => Path.Combine(RootDirectory, "installs.json");

    public static string SchemaDirectory => Path.Combine(RootDirectory, "schemas");

    public static string ManifestPathFor(string version) => Path.Combine(SchemaDirectory, $"corel-{version}.json");

    public static InstallRecord? Load()
    {
        if (!File.Exists(RecordPath))
        {
            return null;
        }
        return InstallRecordSerializer.Deserialize(File.ReadAllText(RecordPath));
    }

    public static void Save(InstallRecord record)
    {
        Directory.CreateDirectory(RootDirectory);
        File.WriteAllText(RecordPath, InstallRecordSerializer.Serialize(record));
    }

    public static CapabilityManifest? LoadManifest(string version)
    {
        string path = ManifestPathFor(version);
        if (!File.Exists(path))
        {
            return null;
        }

        CapabilityManifest manifest = CapabilityManifestSerializer.Deserialize(File.ReadAllText(path));
        if (manifest.SchemaVersion != CapabilityManifest.CurrentSchemaVersion)
        {
            Console.Error.WriteLine(
                $"Warning: cached manifest '{path}' has schema version {manifest.SchemaVersion}, expected {CapabilityManifest.CurrentSchemaVersion}. Ignoring; run `corel schema generate` to regenerate.");
            return null;
        }
        return manifest;
    }

    public static void SaveManifest(CapabilityManifest manifest)
    {
        Directory.CreateDirectory(SchemaDirectory);
        File.WriteAllText(ManifestPathFor(manifest.CorelVersion), CapabilityManifestSerializer.Serialize(manifest));
    }
}
