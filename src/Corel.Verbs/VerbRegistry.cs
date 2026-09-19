using System.Reflection;

namespace Corel.Verbs;

/// <summary>
/// The loaded set of curated verbs. Loads from the embedded verbs.json by default so the
/// registry works regardless of the CLI's working directory (important for a dotnet
/// global tool, which can be invoked from anywhere).
/// </summary>
public sealed class VerbRegistry
{
    public IReadOnlyList<VerbDefinition> Verbs { get; }

    private VerbRegistry(List<VerbDefinition> verbs) => Verbs = verbs;

    public static VerbRegistry FromJson(string json) => new(VerbDefinitionSerializer.Deserialize(json));

    public static VerbRegistry LoadEmbedded()
    {
        Assembly assembly = typeof(VerbRegistry).Assembly;
        const string resourceName = "Corel.Verbs.verbs.json";
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' was not found in {assembly.FullName}.");
        using var reader = new StreamReader(stream);
        return FromJson(reader.ReadToEnd());
    }

    public VerbDefinition? Find(string name) => Verbs.FirstOrDefault(v => v.Name == name);
}
