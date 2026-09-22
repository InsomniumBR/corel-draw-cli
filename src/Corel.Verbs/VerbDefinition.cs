using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corel.Verbs;

/// <summary>
/// One curated verb's mapping to its underlying raw object-model operation. This IS the
/// "extensible mapping" - growing coverage for a new CorelDRAW version or object-model
/// area means adding entries here, not writing a new curated command from scratch.
/// </summary>
/// <remarks>
/// Only RequiresInterface/RequiresMember are consulted by <see cref="VerbAvailability.IsAvailable"/>.
/// Raw is descriptive only (see <see cref="VerbRawBinding"/>) - for a composite verb (one
/// that reads/writes several properties), it names a representative operation, not
/// necessarily the CLI command handler's entire implementation.
/// </remarks>
public sealed class VerbDefinition
{
    public required string Name { get; init; }
    public required string Group { get; init; }
    public required VerbRawBinding Raw { get; init; }
    public string? RequiresInterface { get; init; }
    public string? RequiresMember { get; init; }
}

/// <summary>
/// Documentation only - describes the underlying raw operation for a human reading
/// verbs.json or the README, one of Get/Set/Call matching the verb's operation kind.
/// Nothing at runtime reads these values; only <see cref="VerbDefinition.RequiresInterface"/>
/// and <see cref="VerbDefinition.RequiresMember"/> drive actual behavior.
/// </summary>
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
