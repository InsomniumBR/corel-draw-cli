using Corel.Automation;

namespace Corel.Cli.Commands;

public static class PageCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0 || args[0] != "size")
        {
            Console.Error.WriteLine("Usage: corel page size [--width W --height H] [--unit in|mm|...]");
            return 1;
        }

        var reader = new ArgReader(args[1..]);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object page = ComInvoker.Get(doc, "ActivePage")!;

        string? unitName = reader.Flag("unit");
        if (unitName is not null)
        {
            ComInvoker.Set(doc, "Unit", UnitNames.ToEnumValue(unitName));
        }

        double? width = reader.DoubleOrNull("width");
        double? height = reader.DoubleOrNull("height");

        if (width is not null && height is not null)
        {
            VerbGate.WarnIfUnavailable("page.size.set", corelVersion);
            ComInvoker.Call(page, "SetSize", width.Value, height.Value);
            Console.WriteLine($"Page size set to {width}x{height} ({unitName ?? "current unit"}).");
        }
        else
        {
            object? w = ComInvoker.Get(page, "SizeWidth");
            object? h = ComInvoker.Get(page, "SizeHeight");
            Console.WriteLine($"{w} x {h}");
        }

        return 0;
    }
}
