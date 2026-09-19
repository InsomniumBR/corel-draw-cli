using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corel.Verbs;

/// <summary>
/// One curated verb's mapping to its underlying raw object-model operation. This IS the
/// "extensible mapping" - growing coverage for a new CorelDRAW version or object-model
/// area means adding entries here, not writing a new curated command from scratch.
/// </summary>
/// <remarks>
/// For a composite verb (one that reads/writes several properties, e.g. "doc.info"), Raw
/// is a representative anchor for the availability check, not necessarily the verb's
/// entire implementation - the CLI command handler (Corel.Cli) may make additional calls
/// beyond this single binding. RequiresInterface/RequiresMember drive
/// <see cref="VerbAvailability.IsAvailable"/>.
/// </remarks>
public sealed class VerbDefinition
{
    public required string Name { get; init; }
    public required string Group { get; init; }
    public required VerbRawBinding Raw { get; init; }
    public string? RequiresInterface { get; init; }
    public string? RequiresMember { get; init; }
}

/// <summary>Exactly one of these is normally set, matching the verb's operation kind.</summary>
public sealed class VerbRawBinding
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Get { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Set { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Call { get; init; }
}

public static class VerbDefinitionSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(List<VerbDefinition> verbs)
        => JsonSerializer.Serialize(verbs, Options);

    public static List<VerbDefinition> Deserialize(string json)
        => JsonSerializer.Deserialize<List<VerbDefinition>>(json, Options)
            ?? throw new InvalidDataException("Verb registry JSON deserialized to null.");
}
