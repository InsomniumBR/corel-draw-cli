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
    private static readonly FlagSpec Left = new("left", FlagKind.Value, ValueKind.Number, Required: true);
    private static readonly FlagSpec Top = new("top", FlagKind.Value, ValueKind.Number, Required: true);
    private static readonly FlagSpec Right = new("right", FlagKind.Value, ValueKind.Number, Required: true);
    private static readonly FlagSpec Bottom = new("bottom", FlagKind.Value, ValueKind.Number, Required: true);
    private static readonly FlagSpec Index = new("index", FlagKind.Value, ValueKind.Integer, Required: true);
    private static readonly FlagSpec Indexes = new("indexes", FlagKind.Value, ValueKind.Text, Required: false);
    private static readonly FlagSpec All = new("all", FlagKind.Boolean, ValueKind.Text, Required: false);
    private static readonly FlagSpec Color = new("color", FlagKind.Value, ValueKind.Text, Required: false);
    private static readonly FlagSpec None = new("none", FlagKind.Boolean, ValueKind.Text, Required: false);
    private static readonly FlagSpec Horizontal = new("horizontal", FlagKind.Value, ValueKind.Text, Required: false);
    private static readonly FlagSpec Vertical = new("vertical", FlagKind.Value, ValueKind.Text, Required: false);

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
        new("doc", "export", "corel doc export --as <path> --format png|jpg|svg|pdf [--width W --height H] [--dpi D]",
            "Export the active document to an image or PDF file",
            [new("as", FlagKind.Value, ValueKind.Path, Required: true),
             new("format", FlagKind.Value, ValueKind.Text, Required: true),
             new("width", FlagKind.Value, ValueKind.Number, Required: false),
             new("height", FlagKind.Value, ValueKind.Number, Required: false),
             new("dpi", FlagKind.Value, ValueKind.Number, Required: false)], 0, 0),
        new("doc", "undo", "corel doc undo [--levels N]", "Undo the last N edits on the active document (default 1)",
            [new("levels", FlagKind.Value, ValueKind.Integer, Required: false)], 0, 0),
        new("doc", "redo", "corel doc redo [--levels N]", "Redo the last N undone edits on the active document (default 1)",
            [new("levels", FlagKind.Value, ValueKind.Integer, Required: false)], 0, 0),

        new("docs", "list", "corel docs list", "List open documents, marking the active one", [], 0, 0),
        new("docs", "use", "corel docs use <name|index>", "Make an open document active (errors on an ambiguous name match)", [], 1, 1),
        new("docs", "active", "corel docs active", "Print the active document's name", [], 0, 0),

        new("page", "size", "corel page size [--width W --height H] [--unit U]", "Print or set the active page size",
            [new("width", FlagKind.Value, ValueKind.Number, Required: false),
             new("height", FlagKind.Value, ValueKind.Number, Required: false),
             Unit], 0, 0, RequiredTogether: ["width", "height"]),
        new("page", "add", "corel page add [--after N]", "Add a page (appended at the end, or after page N)",
            [new("after", FlagKind.Value, ValueKind.Integer, Required: false)], 0, 0),
        new("page", "delete", "corel page delete --index N", "Delete a page by its 1-based index",
            [new("index", FlagKind.Value, ValueKind.Integer, Required: true)], 0, 0),
        new("page", "goto", "corel page goto --index N", "Make a page active by its 1-based index",
            [new("index", FlagKind.Value, ValueKind.Integer, Required: true)], 0, 0),
        new("page", "list", "corel page list", "List pages, marking the active one", [], 0, 0),

        new("layer", "list", "corel layer list", "List layers on the active page, marking the active one", [], 0, 0),
        new("layer", "add", "corel layer add --name <name>", "Create a new layer on the active page",
            [new("name", FlagKind.Value, ValueKind.Text, Required: true)], 0, 0),
        new("layer", "use", "corel layer use (--name <name> | --index N)", "Make a layer active",
            [new("name", FlagKind.Value, ValueKind.Text, Required: false),
             new("index", FlagKind.Value, ValueKind.Integer, Required: false)], 0, 0),
        new("layer", "delete", "corel layer delete (--name <name> | --index N)", "Delete a layer from the active page",
            [new("name", FlagKind.Value, ValueKind.Text, Required: false),
             new("index", FlagKind.Value, ValueKind.Integer, Required: false)], 0, 0),

        new("shape", "create-rect",
            "corel shape create-rect --left L --top T --right R --bottom B [--unit U] [--corner-radius N]",
            "Create a rectangle on the active layer (Y-up coordinates)",
            [new("left", FlagKind.Value, ValueKind.Number, Required: true),
             new("top", FlagKind.Value, ValueKind.Number, Required: true),
             new("right", FlagKind.Value, ValueKind.Number, Required: true),
             new("bottom", FlagKind.Value, ValueKind.Number, Required: true),
             Unit,
             new("corner-radius", FlagKind.Value, ValueKind.Number, Required: false)], 0, 0),
        new("shape", "create-ellipse",
            "corel shape create-ellipse --left L --top T --right R --bottom B [--unit U] [--start-angle D] [--end-angle D] [--pie]",
            "Create an ellipse, arc or pie slice on the active layer (Y-up coordinates)",
            [Left, Top, Right, Bottom, Unit,
             new("start-angle", FlagKind.Value, ValueKind.Number, Required: false),
             new("end-angle", FlagKind.Value, ValueKind.Number, Required: false),
             new("pie", FlagKind.Boolean, ValueKind.Text, Required: false)], 0, 0),
        new("shape", "create-polygon",
            "corel shape create-polygon --left L --top T --right R --bottom B --sides N [--unit U] [--star]",
            "Create a regular polygon or star on the active layer (Y-up coordinates)",
            [Left, Top, Right, Bottom,
             new("sides", FlagKind.Value, ValueKind.Integer, Required: true),
             Unit,
             new("star", FlagKind.Boolean, ValueKind.Text, Required: false)], 0, 0),
        new("shape", "create-line", "corel shape create-line --x1 X --y1 Y --x2 X --y2 Y [--unit U]",
            "Create a straight line segment on the active layer (Y-up coordinates)",
            [new("x1", FlagKind.Value, ValueKind.Number, Required: true),
             new("y1", FlagKind.Value, ValueKind.Number, Required: true),
             new("x2", FlagKind.Value, ValueKind.Number, Required: true),
             new("y2", FlagKind.Value, ValueKind.Number, Required: true),
             Unit], 0, 0),
        new("shape", "create-text",
            "corel shape create-text --text <string> --left L --bottom B [--paragraph --top T --right R] [--font F] [--size N] [--bold] [--unit U]",
            "Create artistic text at a point, or paragraph text in a frame with --paragraph",
            [new("text", FlagKind.Value, ValueKind.Text, Required: true),
             Left, Bottom,
             new("paragraph", FlagKind.Boolean, ValueKind.Text, Required: false),
             Top with { Required = false },
             Right with { Required = false },
             new("font", FlagKind.Value, ValueKind.Text, Required: false),
             new("size", FlagKind.Value, ValueKind.Number, Required: false),
             new("bold", FlagKind.Boolean, ValueKind.Text, Required: false),
             Unit], 0, 0),

        new("shape", "list", "corel shape list [--json]", "List every shape on the active page (--json also dumps geometry)",
            [new("json", FlagKind.Boolean, ValueKind.Text, Required: false)], 0, 0),
        new("shape", "delete", "corel shape delete --index N", "Delete a shape by its 1-based position (remaining shapes renumber)",
            [Index], 0, 0),
        new("shape", "rename", "corel shape rename --index N --name <name>", "Rename a shape by its 1-based position",
            [Index, new("name", FlagKind.Value, ValueKind.Text, Required: true)], 0, 0),

        new("shape", "move", "corel shape move --index N (--dx D --dy D | --x X --y Y) [--unit U]",
            "Move a shape by an offset, or to an absolute position",
            [Index,
             new("dx", FlagKind.Value, ValueKind.Number, Required: false),
             new("dy", FlagKind.Value, ValueKind.Number, Required: false),
             new("x", FlagKind.Value, ValueKind.Number, Required: false),
             new("y", FlagKind.Value, ValueKind.Number, Required: false),
             Unit], 0, 0),
        new("shape", "rotate", "corel shape rotate --index N --degrees D [--center-x X --center-y Y] [--unit U]",
            "Rotate a shape, about its own centre or an explicit one",
            [Index,
             new("degrees", FlagKind.Value, ValueKind.Number, Required: true),
             new("center-x", FlagKind.Value, ValueKind.Number, Required: false),
             new("center-y", FlagKind.Value, ValueKind.Number, Required: false),
             Unit], 0, 0),
        new("shape", "scale", "corel shape scale --index N (--width W --height H | --factor F) [--unit U]",
            "Resize a shape to an exact size, or stretch it by a factor",
            [Index,
             new("width", FlagKind.Value, ValueKind.Number, Required: false),
             new("height", FlagKind.Value, ValueKind.Number, Required: false),
             new("factor", FlagKind.Value, ValueKind.Number, Required: false),
             Unit], 0, 0),
        new("shape", "flip", "corel shape flip --index N --axis horizontal|vertical|both",
            "Mirror a shape about one or both axes",
            [Index, new("axis", FlagKind.Value, ValueKind.Text, Required: true)], 0, 0),
        new("shape", "duplicate", "corel shape duplicate --index N [--offset-x X] [--offset-y Y] [--unit U]",
            "Duplicate a shape, optionally offset from the original",
            [Index,
             new("offset-x", FlagKind.Value, ValueKind.Number, Required: false),
             new("offset-y", FlagKind.Value, ValueKind.Number, Required: false),
             Unit], 0, 0),
        new("shape", "order", "corel shape order --index N --to front|back|forward|backward",
            "Change a shape's z-order position",
            [Index, new("to", FlagKind.Value, ValueKind.Text, Required: true)], 0, 0),

        new("shape", "group", "corel shape group (--all | --indexes 1,3,5)", "Group several shapes into one",
            [All, Indexes], 0, 0),
        new("shape", "ungroup", "corel shape ungroup --index N [--all-levels]", "Ungroup a group shape",
            [Index, new("all-levels", FlagKind.Boolean, ValueKind.Text, Required: false)], 0, 0),
        new("shape", "combine", "corel shape combine (--all | --indexes 1,3,5)", "Combine several shapes into one curve",
            [All, Indexes], 0, 0),
        new("shape", "break-apart", "corel shape break-apart --index N", "Split a combined curve back into separate shapes",
            [Index], 0, 0),
        new("shape", "align",
            "corel shape align (--all | --indexes 1,3,5) [--horizontal left|center|right] [--vertical top|center|bottom] [--to shapes|page|page-center|grid]",
            "Align shapes to the first of them (default), or the whole block to the page or grid",
            [All, Indexes, Horizontal, Vertical,
             new("to", FlagKind.Value, ValueKind.Text, Required: false)], 0, 0),
        new("shape", "distribute",
            "corel shape distribute (--all | --indexes 1,3,5) [--horizontal left|center|right|spacing] [--vertical top|center|bottom|spacing] [--over-page]",
            "Space shapes evenly across their own extent, or the whole page with --over-page",
            [All, Indexes, Horizontal, Vertical,
             new("over-page", FlagKind.Boolean, ValueKind.Text, Required: false)], 0, 0),

        new("shape", "fill", "corel shape fill --index N (--color RRGGBB | --none)", "Apply or remove a shape's uniform fill",
            [Index, Color, None], 0, 0),
        new("shape", "outline", "corel shape outline --index N [--width W] [--color RRGGBB] [--none] [--unit U]",
            "Set a shape's outline width and/or colour, or remove it",
            [Index,
             new("width", FlagKind.Value, ValueKind.Number, Required: false),
             Color, None, Unit], 0, 0),

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
