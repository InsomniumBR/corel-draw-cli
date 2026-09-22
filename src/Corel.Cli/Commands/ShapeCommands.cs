using System.Text.Json;
using System.Text.Json.Serialization;
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
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel shape <create-rect|list|delete> ...");
            return 1;
        }

        string sub = args[0];
        var reader = new ArgReader(args[1..]);

        return sub switch
        {
            "create-rect" => CreateRect(app, reader, corelVersion),
            "list" => List(app, reader, corelVersion),
            "delete" => Delete(app, reader),
            _ => Unknown(sub),
        };
    }

    private static int CreateRect(object app, ArgReader reader, string? corelVersion)
    {
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        if (reader.Flag("unit") is string unitName)
        {
            ComInvoker.Set(doc, "Unit", UnitNames.ToEnumValue(unitName));
        }

        double left = reader.Double("left");
        double top = reader.Double("top");
        double right = reader.Double("right");
        double bottom = reader.Double("bottom");
        double corner = reader.DoubleOrNull("corner-radius") ?? 0;

        VerbGate.WarnIfUnavailable("shape.create-rect", corelVersion);
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;
        object rect = ComInvoker.Call(
            layer, "CreateRectangle", left, top, right, bottom, corner, corner, corner, corner)!;

        Console.WriteLine($"Created rectangle at ({left},{bottom})-({right},{top}).");
        return 0;
    }

    private static int List(object app, ArgReader reader, string? corelVersion)
    {
        bool json = reader.Flag("json") is not null;

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;
        object shapes = ComInvoker.Get(layer, "Shapes")!;
        int count = Convert.ToInt32(ComInvoker.Get(shapes, "Count"));
        CapabilityManifest? manifest = VerbGate.GetManifest(corelVersion);

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

    private static int Delete(object app, ArgReader reader)
    {
        string? indexArg = reader.Flag("index");
        if (indexArg is null || !int.TryParse(indexArg, out int index))
        {
            Console.Error.WriteLine("Usage: corel shape delete --index N (1-based, see `corel shape list`)");
            return 1;
        }

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;
        object shapes = ComInvoker.Get(layer, "Shapes")!;
        object shape = ComInvoker.Get(shapes, "Item", index)!;
        ComInvoker.Call(shape, "Delete");
        Console.WriteLine($"Deleted shape at index {index}.");
        return 0;
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown shape subcommand '{sub}'.");
        return 1;
    }
}
