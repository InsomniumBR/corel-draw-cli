using System.Globalization;
using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>
/// Resolves which shape(s) a `shape` subcommand acts on.
/// </summary>
/// <remarks>
/// Indexing is page-wide (<c>ActivePage.Shapes</c>), not active-layer-only: a document with
/// more than one layer made the layer-scoped version report a silently wrong set. Creation
/// still targets the active layer, because CorelDRAW's shape factories live on a layer.
/// </remarks>
public static class ShapeTargets
{
    public static object Document(object app)
        => ComInvoker.ResolvePath(app, "ActiveDocument")!;

    public static object ActiveLayer(object app)
        => ComInvoker.ResolvePath(app, "ActiveDocument.ActiveLayer")!;

    public static object PageShapes(object app)
        => ComInvoker.ResolvePath(app, "ActiveDocument.ActivePage.Shapes")!;

    public static int Count(object shapes)
        => Convert.ToInt32(ComInvoker.Get(shapes, "Count"), CultureInfo.InvariantCulture);

    public static object At(object app, int index)
    {
        object shapes = PageShapes(app);
        RequireInRange(shapes, index);
        return ComInvoker.Get(shapes, "Item", index)!;
    }

    /// <summary>Every shape on the active page, as one range - no selection side effect.</summary>
    public static object AllRange(object app)
        => ComInvoker.Call(PageShapes(app), "All")!;

    /// <summary>A range covering the given 1-based indexes, with no effect on the GUI selection.</summary>
    /// <remarks>
    /// <c>Shapes.Range</c> takes a SAFEARRAY of indexes. That binds fine here because a real
    /// <c>int[]</c> is handed to Type.InvokeMember - it is only unreachable from `raw`, whose
    /// arguments are text. Selecting the shapes and reading back
    /// <c>ActiveDocument.SelectionRange</c> is the equivalent route, rejected because it replaces
    /// whatever the user had selected. (<c>ActiveDocument.Selection</c> is not that route: it
    /// returns an IVGShape, not an IVGShapeRange.)
    /// </remarks>
    public static object Range(object app, IReadOnlyList<int> indexes)
    {
        object shapes = PageShapes(app);
        foreach (int index in indexes)
        {
            RequireInRange(shapes, index);
        }
        return ComInvoker.Call(shapes, "Range", indexes.ToArray())!;
    }

    /// <summary>Resolves `--all` / `--indexes 1,3,5` into one range, rejecting both or neither.</summary>
    public static object RangeFrom(object app, bool all, string? indexes, string usage)
    {
        if (all == (indexes is not null))
        {
            throw new ArgumentException($"Give exactly one of --all or --indexes.{Environment.NewLine}Usage: {usage}");
        }
        return all ? AllRange(app) : Range(app, ParseIndexes(indexes!));
    }

    public static IReadOnlyList<int> ParseIndexes(string value)
    {
        List<int> parsed = [];
        foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) || index < 1)
            {
                throw new ArgumentException($"'{part}' is not a 1-based shape index (expected e.g. --indexes 1,3,5).");
            }
            parsed.Add(index);
        }

        if (parsed.Count == 0)
        {
            throw new ArgumentException("--indexes needs at least one 1-based shape index, e.g. --indexes 1,3,5.");
        }
        return parsed;
    }

    private static void RequireInRange(object shapes, int index)
    {
        int count = Count(shapes);
        if (index < 1 || index > count)
        {
            throw new ArgumentException(
                $"No shape at index {index}: the active page has {count} shape(s). Run `shape list` for current indexes.");
        }
    }
}
