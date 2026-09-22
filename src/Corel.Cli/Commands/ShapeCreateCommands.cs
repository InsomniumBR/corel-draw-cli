using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>The `shape create-*` factories beyond `create-rect`. Every one validates its whole
/// argument list before touching COM, so a bad flag never half-mutates the document.</summary>
public static class ShapeCreateCommands
{
    public static int CreateEllipse(object app, ArgReader reader, string? corelVersion)
    {
        const string usage =
            "corel shape create-ellipse --left L --top T --right R --bottom B [--unit U] "
            + "[--start-angle D] [--end-angle D] [--pie]";
        if (!TryBox(reader, out Box box))
        {
            return ShapeCommands.Usage(usage);
        }

        double? startAngle = reader.DoubleOrNull("start-angle");
        double? endAngle = reader.DoubleOrNull("end-angle");
        bool pie = reader.Flag("pie") is not null;

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.create-ellipse", corelVersion);
        ComInvoker.Call(
            ShapeTargets.ActiveLayer(app), "CreateEllipse",
            box.Left, box.Top, box.Right, box.Bottom, startAngle, endAngle, pie);

        Console.WriteLine($"Created ellipse at ({box.Left},{box.Bottom})-({box.Right},{box.Top}).");
        return 0;
    }

    public static int CreatePolygon(object app, ArgReader reader, string? corelVersion)
    {
        const string usage =
            "corel shape create-polygon --left L --top T --right R --bottom B --sides N [--unit U] [--star]";
        string? sidesArg = reader.Flag("sides");
        if (!TryBox(reader, out Box box)
            || string.IsNullOrEmpty(sidesArg)
            || !int.TryParse(sidesArg, out int sides)
            || sides < 3)
        {
            return ShapeCommands.Usage(usage);
        }
        bool star = reader.Flag("star") is not null;

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.create-polygon", corelVersion);
        ComInvoker.Call(
            ShapeTargets.ActiveLayer(app), "CreatePolygon",
            box.Left, box.Top, box.Right, box.Bottom, sides, null, null, star);

        Console.WriteLine($"Created {(star ? "star" : "polygon")} with {sides} sides.");
        return 0;
    }

    public static int CreateLine(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape create-line --x1 X --y1 Y --x2 X --y2 Y [--unit U]";
        double? x1 = reader.DoubleOrNull("x1");
        double? y1 = reader.DoubleOrNull("y1");
        double? x2 = reader.DoubleOrNull("x2");
        double? y2 = reader.DoubleOrNull("y2");
        if (x1 is null || y1 is null || x2 is null || y2 is null)
        {
            return ShapeCommands.Usage(usage);
        }

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.create-line", corelVersion);
        ComInvoker.Call(
            ShapeTargets.ActiveLayer(app), "CreateLineSegment", x1.Value, y1.Value, x2.Value, y2.Value);

        Console.WriteLine($"Created line ({x1},{y1})-({x2},{y2}).");
        return 0;
    }

    public static int CreateText(object app, ArgReader reader, string? corelVersion)
    {
        const string usage =
            "corel shape create-text --text <string> --left L --bottom B [--paragraph --top T --right R] "
            + "[--font F] [--size N] [--bold] [--unit U]";
        string? text = reader.Flag("text");
        double? left = reader.DoubleOrNull("left");
        double? bottom = reader.DoubleOrNull("bottom");
        if (string.IsNullOrEmpty(text) || left is null || bottom is null)
        {
            return ShapeCommands.Usage(usage);
        }

        bool paragraph = reader.Flag("paragraph") is not null;
        double? top = reader.DoubleOrNull("top");
        double? right = reader.DoubleOrNull("right");
        if (paragraph && (top is null || right is null))
        {
            return ShapeCommands.Usage(usage);
        }

        string? font = reader.Flag("font");
        double? size = reader.DoubleOrNull("size");
        bool bold = reader.Flag("bold") is not null;

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        object layer = ShapeTargets.ActiveLayer(app);
        if (paragraph)
        {
            VerbGate.WarnIfUnavailable("shape.create-text.paragraph", corelVersion);
            ComInvoker.Call(
                layer, "CreateParagraphText",
                left.Value, top!.Value, right!.Value, bottom.Value, text,
                null, null, font, size, bold ? (object?)true : null);
        }
        else
        {
            VerbGate.WarnIfUnavailable("shape.create-text.artistic", corelVersion);
            ComInvoker.Call(
                layer, "CreateArtisticText",
                left.Value, bottom.Value, text,
                null, null, font, size, bold ? (object?)true : null);
        }

        Console.WriteLine($"Created {(paragraph ? "paragraph" : "artistic")} text '{text}'.");
        return 0;
    }

    private readonly record struct Box(double Left, double Top, double Right, double Bottom);

    private static bool TryBox(ArgReader reader, out Box box)
    {
        box = default;
        double? left = reader.DoubleOrNull("left");
        double? top = reader.DoubleOrNull("top");
        double? right = reader.DoubleOrNull("right");
        double? bottom = reader.DoubleOrNull("bottom");
        if (left is null || top is null || right is null || bottom is null)
        {
            return false;
        }
        box = new Box(left.Value, top.Value, right.Value, bottom.Value);
        return true;
    }
}
