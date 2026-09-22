using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>Fill and outline for one shape.</summary>
/// <remarks>
/// Colour is applied by fetching the live <c>IVGColor</c> off the shape and calling
/// <c>RGBAssign</c> on it, rather than building a colour object and passing it to
/// <c>ApplyUniformFill</c>: the object-argument route does not bind late-bound, while
/// assigning into the existing colour does and also switches a no-fill shape to a uniform fill.
/// </remarks>
public static class ShapeStyleCommands
{
    public static int Fill(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape fill --index N (--color RRGGBB | --none)";
        string? color = reader.Flag("color");
        bool none = reader.Flag("none") is not null;
        if (!ShapeCommands.TryIndex(reader, out int index) || none == (color is not null && color.Length > 0))
        {
            return ShapeCommands.Usage(usage);
        }

        RgbColor? parsed = none ? null : RgbColor.Parse(color!);

        VerbGate.WarnIfUnavailable("shape.fill", corelVersion);
        object fill = ComInvoker.Get(ShapeTargets.At(app, index), "Fill")!;
        if (parsed is null)
        {
            ComInvoker.Call(fill, "ApplyNoFill");
            Console.WriteLine($"Removed the fill from shape {index}.");
        }
        else
        {
            Assign(ComInvoker.Get(fill, "UniformColor")!, parsed.Value);
            Console.WriteLine($"Filled shape {index} with {parsed.Value}.");
        }
        return 0;
    }

    public static int Outline(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape outline --index N [--width W] [--color RRGGBB] [--none] [--unit U]";
        string? color = reader.Flag("color");
        double? width = reader.DoubleOrNull("width");
        bool none = reader.Flag("none") is not null;
        bool styled = width is not null || (color is not null && color.Length > 0);
        if (!ShapeCommands.TryIndex(reader, out int index) || none == styled)
        {
            return ShapeCommands.Usage(usage);
        }

        RgbColor? parsed = color is { Length: > 0 } ? RgbColor.Parse(color) : null;

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.outline", corelVersion);
        object outline = ComInvoker.Get(ShapeTargets.At(app, index), "Outline")!;
        if (none)
        {
            ComInvoker.Call(outline, "SetNoOutline");
            Console.WriteLine($"Removed the outline from shape {index}.");
            return 0;
        }

        if (width is not null)
        {
            ComInvoker.Set(outline, "Width", width.Value);
        }
        if (parsed is not null)
        {
            Assign(ComInvoker.Get(outline, "Color")!, parsed.Value);
        }

        Console.WriteLine(
            $"Outlined shape {index}"
            + (width is not null ? $" at width {width}" : "")
            + (parsed is not null ? $" in {parsed.Value}" : "")
            + ".");
        return 0;
    }

    private static void Assign(object comColor, RgbColor color)
        => ComInvoker.Call(comColor, "RGBAssign", color.Red, color.Green, color.Blue);
}
