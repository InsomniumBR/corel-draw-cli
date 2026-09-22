using System.Text.Json;
using Corel.Automation;
using Corel.Schema;

namespace Corel.Cli.Commands;

public static class ShapeCommands
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string[] args, object app, string? corelVersion)
        => Run(args, app, corelVersion, VerbGate.GetManifest(corelVersion));

    /// <summary>Entry point taking an explicit manifest, so enum-backed subcommands are testable
    /// without a cached manifest on disk.</summary>
    public static int Run(string[] args, object app, string? corelVersion, CapabilityManifest? manifest)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine(
                "Usage: corel shape <" + string.Join("|", CommandGrammar.SubcommandsOf("shape")) + "> ...");
            return 1;
        }

        string sub = args[0];
        var reader = new ArgReader(args[1..]);

        return sub switch
        {
            "create-rect" => CreateRect(app, reader, corelVersion),
            "create-ellipse" => ShapeCreateCommands.CreateEllipse(app, reader, corelVersion),
            "create-polygon" => ShapeCreateCommands.CreatePolygon(app, reader, corelVersion),
            "create-line" => ShapeCreateCommands.CreateLine(app, reader, corelVersion),
            "create-text" => ShapeCreateCommands.CreateText(app, reader, corelVersion),
            "list" => List(app, reader, manifest, corelVersion),
            "delete" => Delete(app, reader, corelVersion),
            "rename" => Rename(app, reader),
            "move" => ShapeTransformCommands.Move(app, reader, corelVersion),
            "rotate" => ShapeTransformCommands.Rotate(app, reader, corelVersion),
            "scale" => ShapeTransformCommands.Scale(app, reader, corelVersion),
            "flip" => ShapeTransformCommands.Flip(app, reader, corelVersion, manifest),
            "duplicate" => ShapeTransformCommands.Duplicate(app, reader, corelVersion),
            "order" => ShapeTransformCommands.Order(app, reader, corelVersion),
            "group" => ShapeArrangeCommands.Group(app, reader, corelVersion),
            "ungroup" => ShapeArrangeCommands.Ungroup(app, reader, corelVersion),
            "combine" => ShapeArrangeCommands.Combine(app, reader, corelVersion),
            "break-apart" => ShapeArrangeCommands.BreakApart(app, reader, corelVersion),
            "align" => ShapeArrangeCommands.Align(app, reader, corelVersion, manifest),
            "distribute" => ShapeArrangeCommands.Distribute(app, reader, corelVersion, manifest),
            "fill" => ShapeStyleCommands.Fill(app, reader, corelVersion),
            "outline" => ShapeStyleCommands.Outline(app, reader, corelVersion),
            _ => Unknown(sub),
        };
    }

    /// <summary>Applies `--unit` to the document before any geometry is created, so the numbers
    /// on the same command line are read in the unit the caller meant.</summary>
    internal static void ApplyUnit(object app, ArgReader reader, string? corelVersion)
    {
        if (reader.Flag("unit") is not string unitName || unitName.Length == 0)
        {
            return;
        }
        VerbGate.WarnIfUnavailable("doc.unit.set", corelVersion);
        ComInvoker.Set(ShapeTargets.Document(app), "Unit", UnitNames.ToEnumValue(unitName));
    }

    internal static int Usage(string usage)
    {
        Console.Error.WriteLine($"Usage: {usage}");
        return 1;
    }

    internal static bool TryIndex(ArgReader reader, out int index)
    {
        index = 0;
        string? raw = reader.Flag("index");
        return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out index);
    }

    private static int CreateRect(object app, ArgReader reader, string? corelVersion)
    {
        const string usage =
            "corel shape create-rect --left L --top T --right R --bottom B [--unit U] [--corner-radius N]";
        double? left = reader.DoubleOrNull("left");
        double? top = reader.DoubleOrNull("top");
        double? right = reader.DoubleOrNull("right");
        double? bottom = reader.DoubleOrNull("bottom");
        if (left is null || top is null || right is null || bottom is null)
        {
            return Usage(usage);
        }
        double corner = reader.DoubleOrNull("corner-radius") ?? 0;

        ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.create-rect", corelVersion);
        object layer = ShapeTargets.ActiveLayer(app);
        ComInvoker.Call(
            layer, "CreateRectangle", left.Value, top.Value, right.Value, bottom.Value, corner, corner, corner, corner);

        Console.WriteLine($"Created rectangle at ({left},{bottom})-({right},{top}).");
        return 0;
    }

    private static int List(object app, ArgReader reader, CapabilityManifest? manifest, string? corelVersion)
    {
        VerbGate.WarnIfUnavailable("shape.list", corelVersion);
        bool json = reader.Flag("json") is not null;

        object shapes = ShapeTargets.PageShapes(app);
        int count = ShapeTargets.Count(shapes);

        List<ShapeListEntry>? entries = json ? [] : null;
        for (int i = 1; i <= count; i++)
        {
            object shape = ComInvoker.Get(shapes, "Item", i)!;
            object? rawType = ComInvoker.Get(shape, "Type");
            object? name = ComInvoker.Get(shape, "Name");
            string typeName = ShapeTypeNames.Resolve(rawType, manifest);

            if (entries is not null)
            {
                entries.Add(new ShapeListEntry(
                    i,
                    typeName,
                    name?.ToString(),
                    Convert.ToDouble(ComInvoker.Get(shape, "LeftX")),
                    Convert.ToDouble(ComInvoker.Get(shape, "TopY")),
                    Convert.ToDouble(ComInvoker.Get(shape, "RightX")),
                    Convert.ToDouble(ComInvoker.Get(shape, "BottomY")),
                    Convert.ToDouble(ComInvoker.Get(shape, "SizeWidth")),
                    Convert.ToDouble(ComInvoker.Get(shape, "SizeHeight"))));
            }
            else
            {
                Console.WriteLine($"[{i}] type={typeName} name={name}");
            }
        }

        if (entries is not null)
        {
            Console.WriteLine(JsonSerializer.Serialize(entries, JsonOptions));
        }
        return 0;
    }

    private sealed record ShapeListEntry(
        int Index,
        string Type,
        string? Name,
        double LeftX,
        double TopY,
        double RightX,
        double BottomY,
        double SizeWidth,
        double SizeHeight);

    private static int Delete(object app, ArgReader reader, string? corelVersion)
    {
        if (!TryIndex(reader, out int index))
        {
            return Usage("corel shape delete --index N (1-based, see `corel shape list`)");
        }

        VerbGate.WarnIfUnavailable("shape.delete", corelVersion);
        ComInvoker.Call(ShapeTargets.At(app, index), "Delete");
        Console.WriteLine(
            $"Deleted shape at index {index}. Remaining shapes have renumbered - re-run `shape list` " +
            "before deleting another by index.");
        return 0;
    }

    private static int Rename(object app, ArgReader reader)
    {
        const string usage = "corel shape rename --index N --name <name> (1-based, see `corel shape list`)";
        string? name = reader.Flag("name");
        if (!TryIndex(reader, out int index) || string.IsNullOrEmpty(name))
        {
            return Usage(usage);
        }

        ComInvoker.Set(ShapeTargets.At(app, index), "Name", name);
        Console.WriteLine($"Renamed shape {index} to '{name}'.");
        return 0;
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown shape subcommand '{sub}'.");
        return 1;
    }
}
