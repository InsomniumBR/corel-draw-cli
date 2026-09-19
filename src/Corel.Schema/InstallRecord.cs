using System.Text.Json;

namespace Corel.Schema;

/// <summary>
/// One CorelDRAW installation detected on this machine.
/// </summary>
public sealed class CorelInstall
{
    public required string Version { get; init; }
    public required string ProgId { get; init; }
    public required string InstallPath { get; init; }
    public required string TlbPath { get; init; }

    /// <summary>Populated once `corel schema generate` has cached a manifest for this version.</summary>
    public string? ManifestPath { get; set; }
}

/// <summary>
/// The local discovery record - every CorelDRAW install this machine has found, and which
/// one is used when `--corel-version` is omitted. Cached at
/// %LOCALAPPDATA%\corel-draw-cli\installs.json so detection doesn't re-run registry scans
/// on every invocation.
/// </summary>
public sealed class InstallRecord
{
    public string? DefaultVersion { get; set; }
    public List<CorelInstall> Installs { get; init; } = [];
}

public static class InstallRecordSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(InstallRecord record)
        => JsonSerializer.Serialize(record, Options);

    public static InstallRecord Deserialize(string json)
        => JsonSerializer.Deserialize<InstallRecord>(json, Options)
            ?? throw new InvalidDataException("Install record JSON deserialized to null.");
}
