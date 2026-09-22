namespace Corel.Cli;

public enum FlagKind
{
    Boolean,
    Value,
}

public enum ValueKind
{
    Text,
    Number,
    Integer,
    Unit,
    Path,
}

public sealed record FlagSpec(string Name, FlagKind Kind, ValueKind Value, bool Required);

public sealed record CommandSpec(
    string Group,
    string? Sub,
    string Usage,
    string Summary,
    IReadOnlyList<FlagSpec> Flags,
    int MinPositional,
    int MaxPositional,
    IReadOnlyList<string>? RequiredTogether = null,
    bool ArbitraryTrailingArgs = false)
{
    public string Name => Sub is null ? Group : $"{Group} {Sub}";

    public int Words => Sub is null ? 1 : 2;

    public FlagSpec? FindFlag(string name)
        => Flags.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.Ordinal));
}

/// <summary>The single declarative description of every command the CLI accepts - help text,
/// structural validation and the script preflight all read this one table.</summary>
public static class CommandGrammar
{
    private static readonly FlagSpec Unit = new("unit", FlagKind.Value, ValueKind.Unit, Required: false);

    public static IReadOnlyList<CommandSpec> Commands { get; } =
    [
        new("help", null, "corel help [command]", "Show this help, or help for one command", [], 0, 2, ArbitraryTrailingArgs: true),
        new("detect", null, "corel detect", "Find installed CorelDRAW version(s) via the registry", [], 0, 0),
        new("schema", "generate", "corel schema generate [--version N]", "Cache a version's object-model capability manifest",
            [new("version", FlagKind.Value, ValueKind.Text, Required: false)], 0, 0),

        new("doc", "new", "corel doc new", "Create a blank document", [], 0, 0),
        new("doc", "save", "corel doc save [--as <path>] [--embed-vba]", "Save the active document (SaveAs when --as is given)",
            [new("as", FlagKind.Value, ValueKind.Path, Required: false),
             new("embed-vba", FlagKind.Boolean, ValueKind.Text, Required: false)], 0, 0),

        new("docs", "list", "corel docs list", "List open documents, marking the active one", [], 0, 0),
        new("docs", "use", "corel docs use <name|index>", "Make an open document active (errors on an ambiguous name match)", [], 1, 1),
        new("docs", "active", "corel docs active", "Print the active document's name", [], 0, 0),

        new("page", "size", "corel page size [--width W --height H] [--unit U]", "Print or set the active page size",
            [new("width", FlagKind.Value, ValueKind.Number, Required: false),
             new("height", FlagKind.Value, ValueKind.Number, Required: false),
             Unit], 0, 0, RequiredTogether: ["width", "height"]),

        new("shape", "create-rect",
            "corel shape create-rect --left L --top T --right R --bottom B [--unit U] [--corner-radius N]",
            "Create a rectangle on the active layer (Y-up coordinates)",
            [new("left", FlagKind.Value, ValueKind.Number, Required: true),
             new("top", FlagKind.Value, ValueKind.Number, Required: true),
             new("right", FlagKind.Value, ValueKind.Number, Required: true),
             new("bottom", FlagKind.Value, ValueKind.Number, Required: true),
             Unit,
             new("corner-radius", FlagKind.Value, ValueKind.Number, Required: false)], 0, 0),
        new("shape", "list", "corel shape list [--json]", "List shapes on the active layer (--json also dumps geometry)",
            [new("json", FlagKind.Boolean, ValueKind.Text, Required: false)], 0, 0),
        new("shape", "delete", "corel shape delete --index N", "Delete a shape by its 1-based position (remaining shapes renumber)",
            [new("index", FlagKind.Value, ValueKind.Integer, Required: true)], 0, 0),
        new("shape", "rename", "corel shape rename --index N --name <name>", "Rename a shape by its 1-based position",
            [new("index", FlagKind.Value, ValueKind.Integer, Required: true),
             new("name", FlagKind.Value, ValueKind.Text, Required: true)], 0, 0),

        new("macro", "install", "corel macro install --name <Module> --file <path.bas>",
            "Inject VBA source into the active document's own VBA project",
            [new("name", FlagKind.Value, ValueKind.Text, Required: true),
             new("file", FlagKind.Value, ValueKind.Path, Required: true)], 0, 0),

        new("image", "import",
            "corel image import --file <path> (--into <shapeName> | --left L --top T --right R --bottom B) [--unit U]",
            "Import an image onto the active layer, fit to a shape or explicit bounds",
            [new("file", FlagKind.Value, ValueKind.Path, Required: true),
             new("into", FlagKind.Value, ValueKind.Text, Required: false),
             new("left", FlagKind.Value, ValueKind.Number, Required: false),
             new("top", FlagKind.Value, ValueKind.Number, Required: false),
             new("right", FlagKind.Value, ValueKind.Number, Required: false),
             new("bottom", FlagKind.Value, ValueKind.Number, Required: false),
             Unit], 0, 0),

        new("raw", "get", "corel raw get <dotted.path>", "Read any member of the object model", [], 1, 1),
        new("raw", "set", "corel raw set <dotted.path> <value>", "Write any member of the object model", [], 2, 2),
        new("raw", "call", "corel raw call <dotted.path> [args...]", "Invoke any method of the object model",
            [], 1, int.MaxValue, ArbitraryTrailingArgs: true),

        new("script", "run", "corel script run <file.cdx> [--vars K=V,K2=V2] [--force]", "Run a .cdx script",
            [new("vars", FlagKind.Value, ValueKind.Text, Required: false),
             new("force", FlagKind.Boolean, ValueKind.Text, Required: false)], 1, 1),

        new("shell", null, "corel shell", "Interactive REPL on one persistent connection", [], 0, 0),
    ];

    public static IReadOnlyList<string> Groups { get; } = [.. Commands.Select(c => c.Group).Distinct()];

    public static bool IsGroup(string group) => Groups.Contains(group, StringComparer.Ordinal);

    public static IReadOnlyList<CommandSpec> InGroup(string group)
        => [.. Commands.Where(c => string.Equals(c.Group, group, StringComparison.Ordinal))];

    public static IReadOnlyList<string> SubcommandsOf(string group)
        => [.. InGroup(group).Where(c => c.Sub is not null).Select(c => c.Sub!)];

    public static CommandSpec? Find(string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return null;
        }

        IReadOnlyList<CommandSpec> group = InGroup(tokens[0]);
        if (group.Count == 0)
        {
            return null;
        }

        CommandSpec? single = group.FirstOrDefault(c => c.Sub is null);
        if (single is not null)
        {
            return single;
        }

        return tokens.Length < 2
            ? null
            : group.FirstOrDefault(c => string.Equals(c.Sub, tokens[1], StringComparison.Ordinal));
    }
}
