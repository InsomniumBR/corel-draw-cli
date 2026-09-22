using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corel.Schema;

/// <summary>
/// Everything a specific installed CorelDRAW version's type library exposes, as scanned
/// by <see cref="TypeLibScanner"/>. Cached on disk per-version so the CLI doesn't need to
/// re-scan the .tlb on every invocation.
/// </summary>
public sealed class CapabilityManifest
{
    /// <summary>Bump when this class's shape changes incompatibly; checked by <see cref="Corel.Automation.InstallStore.LoadManifest"/>.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string CorelVersion { get; init; }
    public required string ProgId { get; init; }
    public required string TlbPath { get; init; }
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public Dictionary<string, InterfaceInfo> Interfaces { get; init; } = [];
}

public sealed class InterfaceInfo
{
    /// <summary>TKIND_DISPATCH, TKIND_INTERFACE, or TKIND_ENUM.</summary>
    public required string Kind { get; init; }
    public Dictionary<string, MemberInfo> Members { get; init; } = [];
}

public sealed class MemberInfo
{
    /// <summary>"method", "property", or "const" (enum member).</summary>
    public required string Kind { get; init; }
    public List<string> Params { get; init; } = [];

    /// <summary>Set only for "const" members (enum values).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Value { get; init; }
}

public static class CapabilityManifestSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(CapabilityManifest manifest)
        => JsonSerializer.Serialize(manifest, Options);

    public static CapabilityManifest Deserialize(string json)
        => JsonSerializer.Deserialize<CapabilityManifest>(json, Options)
            ?? throw new InvalidDataException("Manifest JSON deserialized to null.");
}
