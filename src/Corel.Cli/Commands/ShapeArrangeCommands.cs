using Corel.Automation;
using Corel.Schema;

namespace Corel.Cli.Commands;

/// <summary>Multi-shape operations: grouping, combining, align and distribute. Each acts on an
/// IVGShapeRange built by <see cref="ShapeTargets.Range"/>.</summary>
public static class ShapeArrangeCommands
{
    private const string AlignEnum = "cdrAlignType";
    private const string DistributeEnum = "cdrDistributeType";
    private const string ToShapes = "shapes";

    private static readonly Dictionary<string, string> HorizontalAlign = new(StringComparer.Ordinal)
    {
        ["left"] = "cdrAlignLeft",
        ["center"] = "cdrAlignHCenter",
        ["right"] = "cdrAlignRight",
    };

    private static readonly Dictionary<string, string> VerticalAlign = new(StringComparer.Ordinal)
    {
        ["top"] = "cdrAlignTop",
        ["center"] = "cdrAlignVCenter",
        ["bottom"] = "cdrAlignBottom",
    };

    private static readonly Dictionary<string, string> HorizontalDistribute = new(StringComparer.Ordinal)
    {
        ["left"] = "cdrDistributeLeft",
        ["center"] = "cdrDistributeHCenter",
        ["right"] = "cdrDistributeRight",
        ["spacing"] = "cdrDistributeHSpacing",
    };

    private static readonly Dictionary<string, string> VerticalDistribute = new(StringComparer.Ordinal)
    {
        ["top"] = "cdrDistributeTop",
        ["center"] = "cdrDistributeVCenter",
        ["bottom"] = "cdrDistributeBottom",
        ["spacing"] = "cdrDistributeVSpacing",
    };

    /// <summary>
    /// What each `--to` word actually calls. Only the `AlignRangeTo*` members move shapes relative
    /// to the page: verified on X7, <c>IVGShapeRange.AlignToPage</c> aligns the range to its own
    /// first shape (not the page) and <c>IVGShape.AlignToPage</c> does nothing at all, so neither
    /// is used here however apt the name looks.
    /// </summary>
    private static readonly Dictionary<string, string> AlignTargets = new(StringComparer.Ordinal)
    {
        [ToShapes] = "AlignToShape",
        ["page"] = "AlignRangeToPage",
        ["page-center"] = "AlignRangeToPageCenter",
        ["grid"] = "AlignRangeToGrid",
    };

    public static int Group(object app, ArgReader reader, string? corelVersion)
        => RangeOperation(app, reader, corelVersion, "shape.group", "Group",
            "corel shape group (--all | --indexes 1,3,5)", "Grouped");

    public static int Combine(object app, ArgReader reader, string? corelVersion)
        => RangeOperation(app, reader, corelVersion, "shape.combine", "Combine",
            "corel shape combine (--all | --indexes 1,3,5)", "Combined");

    public static int Ungroup(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape ungroup --index N [--all-levels]";
        if (!ShapeCommands.TryIndex(reader, out int index))
        {
            return ShapeCommands.Usage(usage);
        }
        bool allLevels = reader.Flag("all-levels") is not null;

        VerbGate.WarnIfUnavailable("shape.ungroup", corelVersion);
        ComInvoker.Call(ShapeTargets.At(app, index), allLevels ? "UngroupAll" : "Ungroup");
        Console.WriteLine($"Ungrouped shape {index}. Indexes have shifted - re-run `shape list`.");
        return 0;
    }

    public static int BreakApart(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape break-apart --index N";
        if (!ShapeCommands.TryIndex(reader, out int index))
        {
            return ShapeCommands.Usage(usage);
        }

        VerbGate.WarnIfUnavailable("shape.break-apart", corelVersion);
        ComInvoker.Call(ShapeTargets.At(app, index), "BreakApart");
        Console.WriteLine($"Broke apart shape {index}. Indexes have shifted - re-run `shape list`.");
        return 0;
    }

    public static int Align(object app, ArgReader reader, string? corelVersion, CapabilityManifest? manifest)
    {
        const string usage =
            "corel shape align (--all | --indexes 1,3,5) [--horizontal left|center|right] "
            + "[--vertical top|center|bottom] [--to shapes|page|page-center|grid]";
        string? horizontal = reader.Flag("horizontal");
        string? vertical = reader.Flag("vertical");
        string to = reader.Flag("to") ?? ToShapes;
        if ((horizontal is null && vertical is null) || !AlignTargets.TryGetValue(to, out string? method))
        {
            return ShapeCommands.Usage(usage);
        }

        long type = Combine(manifest, AlignEnum, HorizontalAlign, VerticalAlign, horizontal, vertical);

        object range = ShapeTargets.RangeFrom(app, reader.Flag("all") is not null, reader.Flag("indexes"), usage);
        int count = ShapeTargets.Count(range);
        VerbGate.WarnIfUnavailable("shape.align", corelVersion);
        if (to == ToShapes)
        {
            ComInvoker.Call(range, method, (int)type, ComInvoker.Get(range, "FirstShape")!);
        }
        else
        {
            ComInvoker.Call(range, method, (int)type);
        }

        Console.WriteLine(
            to == ToShapes
                ? $"Aligned {count} shape(s) to the first of them."
                : $"Aligned {count} shape(s) as one block to the {to}.");
        return 0;
    }

    public static int Distribute(object app, ArgReader reader, string? corelVersion, CapabilityManifest? manifest)
    {
        const string usage =
            "corel shape distribute (--all | --indexes 1,3,5) [--horizontal left|center|right|spacing] "
            + "[--vertical top|center|bottom|spacing] [--over-page]";
        string? horizontal = reader.Flag("horizontal");
        string? vertical = reader.Flag("vertical");
        if (horizontal is null && vertical is null)
        {
            return ShapeCommands.Usage(usage);
        }

        bool overPage = reader.Flag("over-page") is not null;
        long type = Combine(manifest, DistributeEnum, HorizontalDistribute, VerticalDistribute, horizontal, vertical);

        object range = ShapeTargets.RangeFrom(app, reader.Flag("all") is not null, reader.Flag("indexes"), usage);
        int count = ShapeTargets.Count(range);
        VerbGate.WarnIfUnavailable("shape.distribute", corelVersion);
        ComInvoker.Call(range, "Distribute", (int)type, overPage);
        Console.WriteLine(
            $"Distributed {count} shape(s) over the {(overPage ? "page" : "shapes' own extent")}.");
        return 0;
    }

    /// <summary>ORs the chosen horizontal and vertical options together - both enums pack the two
    /// axes into disjoint bit ranges of one value.</summary>
    private static long Combine(
        CapabilityManifest? manifest,
        string enumName,
        IReadOnlyDictionary<string, string> horizontalOptions,
        IReadOnlyDictionary<string, string> verticalOptions,
        string? horizontal,
        string? vertical)
    {
        long type = 0;
        if (horizontal is not null)
        {
            type |= CorelEnums.FromOption(manifest, enumName, horizontalOptions, "horizontal", horizontal);
        }
        if (vertical is not null)
        {
            type |= CorelEnums.FromOption(manifest, enumName, verticalOptions, "vertical", vertical);
        }
        return type;
    }

    private static int RangeOperation(
        object app, ArgReader reader, string? corelVersion, string verb, string method, string usage, string past)
    {
        object range = ShapeTargets.RangeFrom(app, reader.Flag("all") is not null, reader.Flag("indexes"), usage);
        int count = ShapeTargets.Count(range);
        VerbGate.WarnIfUnavailable(verb, corelVersion);
        ComInvoker.Call(range, method);
        Console.WriteLine($"{past} {count} shape(s). Indexes have shifted - re-run `shape list`.");
        return 0;
    }
}
