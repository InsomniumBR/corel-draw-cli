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
        string? unitName = reader.Flag("unit");
        int? unitValue = unitName is not null ? UnitNames.ToEnumValue(unitName) : null;

        double left = reader.Double("left");
        double top = reader.Double("top");
        double right = reader.Double("right");
        double bottom = reader.Double("bottom");
        double corner = reader.DoubleOrNull("corner-radius") ?? 0;

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        if (unitValue is not null)
        {
            ComInvoker.Set(doc, "Unit", unitValue.Value);
        }

        VerbGate.WarnIfUnavailable("shape.create-rect", corelVersion);
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;
        object rect = ComInvoker.Call(
            layer, "CreateRectangle", left, top, right, bottom, corner, corner, corner, corner)!;

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
