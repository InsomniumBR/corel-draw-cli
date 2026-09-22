using Corel.Automation;

namespace Corel.Cli.Commands;

public static class ShapeCommands
{
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
            "list" => List(app),
            "delete" => Delete(app, reader),
            _ => Unknown(sub),
        };
    }

    private static int CreateRect(object app, ArgReader reader, string? corelVersion)
    {
        double? left = reader.DoubleOrNull("left");
        double? top = reader.DoubleOrNull("top");
        double? right = reader.DoubleOrNull("right");
        double? bottom = reader.DoubleOrNull("bottom");
        if (left is null || top is null || right is null || bottom is null)
        {
            Console.Error.WriteLine(
                "Usage: corel shape create-rect --left L --top T --right R --bottom B [--unit U] [--corner-radius N]");
            return 1;
        }
        double corner = reader.DoubleOrNull("corner-radius") ?? 0;

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        if (reader.Flag("unit") is string unitName)
        {
            ComInvoker.Set(doc, "Unit", UnitNames.ToEnumValue(unitName));
        }

        VerbGate.WarnIfUnavailable("shape.create-rect", corelVersion);
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;
        object rect = ComInvoker.Call(
            layer, "CreateRectangle", left.Value, top.Value, right.Value, bottom.Value, corner, corner, corner, corner)!;

        Console.WriteLine($"Created rectangle at ({left},{bottom})-({right},{top}).");
        return 0;
    }

    private static int List(object app)
    {
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;
        object shapes = ComInvoker.Get(layer, "Shapes")!;
        int count = Convert.ToInt32(ComInvoker.Get(shapes, "Count"));

        for (int i = 1; i <= count; i++)
        {
            object shape = ComInvoker.Get(shapes, "Item", i)!;
            object? type = ComInvoker.Get(shape, "Type");
            object? name = ComInvoker.Get(shape, "Name");
            Console.WriteLine($"[{i}] type={type} name={name}");
        }
        return 0;
    }

    private static int Delete(object app, ArgReader reader)
    {
        string? indexArg = reader.Flag("index");
        if (string.IsNullOrEmpty(indexArg) || !int.TryParse(indexArg, out int index))
        {
            Console.Error.WriteLine("Usage: corel shape delete --index N (1-based, see `corel shape list`)");
            return 1;
        }

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;
        object shapes = ComInvoker.Get(layer, "Shapes")!;
        object shape = ComInvoker.Get(shapes, "Item", index)!;
        ComInvoker.Call(shape, "Delete");
        Console.WriteLine(
            $"Deleted shape at index {index}. Remaining shapes have renumbered - re-run `shape list` " +
            "before deleting another by index.");
        return 0;
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown shape subcommand '{sub}'.");
        return 1;
    }
}
