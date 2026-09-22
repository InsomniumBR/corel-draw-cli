using Corel.Automation;
using Corel.Schema;

namespace Corel.Cli.Commands;

/// <summary>Per-shape transforms and z-order, all addressed by the same 1-based page index
/// `shape list` prints.</summary>
public static class ShapeTransformCommands
{
    private static readonly Dictionary<string, string> FlipAxes = new(StringComparer.Ordinal)
    {
        ["horizontal"] = "cdrFlipHorizontal",
        ["vertical"] = "cdrFlipVertical",
        ["both"] = "cdrFlipBoth",
    };

    private static readonly Dictionary<string, string> OrderMethods = new(StringComparer.Ordinal)
    {
        ["front"] = "OrderToFront",
        ["back"] = "OrderToBack",
        ["forward"] = "OrderForwardOne",
        ["backward"] = "OrderBackOne",
    };

    public static int Move(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape move --index N (--dx D --dy D | --x X --y Y) [--unit U]";
        double? dx = reader.DoubleOrNull("dx");
        double? dy = reader.DoubleOrNull("dy");
        double? x = reader.DoubleOrNull("x");
        double? y = reader.DoubleOrNull("y");
        bool relative = dx is not null || dy is not null;
        bool absolute = x is not null || y is not null;
        if (!ShapeCommands.TryIndex(reader, out int index) || relative == absolute)
        {
            return ShapeCommands.Usage(usage);
        }

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.move", corelVersion);
        object shape = ShapeTargets.At(app, index);
        if (relative)
        {
            ComInvoker.Call(shape, "Move", dx ?? 0, dy ?? 0);
            Console.WriteLine($"Moved shape {index} by ({dx ?? 0},{dy ?? 0}).");
        }
        else
        {
            ComInvoker.Call(
                shape, "SetPosition",
                x ?? Convert.ToDouble(ComInvoker.Get(shape, "PositionX")),
                y ?? Convert.ToDouble(ComInvoker.Get(shape, "PositionY")));
            Console.WriteLine($"Moved shape {index} to ({x?.ToString() ?? "-"},{y?.ToString() ?? "-"}).");
        }
        return 0;
    }

    public static int Rotate(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape rotate --index N --degrees D [--center-x X --center-y Y] [--unit U]";
        double? degrees = reader.DoubleOrNull("degrees");
        double? centerX = reader.DoubleOrNull("center-x");
        double? centerY = reader.DoubleOrNull("center-y");
        if (!ShapeCommands.TryIndex(reader, out int index)
            || degrees is null
            || (centerX is null) != (centerY is null))
        {
            return ShapeCommands.Usage(usage);
        }

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.rotate", corelVersion);
        object shape = ShapeTargets.At(app, index);
        if (centerX is null)
        {
            ComInvoker.Call(shape, "Rotate", degrees.Value);
        }
        else
        {
            ComInvoker.Call(shape, "RotateEx", degrees.Value, centerX.Value, centerY!.Value);
        }

        Console.WriteLine($"Rotated shape {index} by {degrees} degrees.");
        return 0;
    }

    public static int Scale(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape scale --index N (--width W --height H | --factor F) [--unit U]";
        double? width = reader.DoubleOrNull("width");
        double? height = reader.DoubleOrNull("height");
        double? factor = reader.DoubleOrNull("factor");
        bool bySize = width is not null && height is not null;
        if (!ShapeCommands.TryIndex(reader, out int index) || bySize == (factor is not null))
        {
            return ShapeCommands.Usage(usage);
        }

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.scale", corelVersion);
        object shape = ShapeTargets.At(app, index);
        if (bySize)
        {
            ComInvoker.Call(shape, "SetSize", width!.Value, height!.Value);
            Console.WriteLine($"Resized shape {index} to {width} x {height}.");
        }
        else
        {
            ComInvoker.Call(shape, "Stretch", factor!.Value, factor.Value, true);
            Console.WriteLine($"Scaled shape {index} by {factor}.");
        }
        return 0;
    }

    public static int Flip(object app, ArgReader reader, string? corelVersion, CapabilityManifest? manifest)
    {
        const string usage = "corel shape flip --index N --axis horizontal|vertical|both";
        string? axis = reader.Flag("axis");
        if (!ShapeCommands.TryIndex(reader, out int index) || string.IsNullOrEmpty(axis))
        {
            return ShapeCommands.Usage(usage);
        }

        long axes = CorelEnums.FromOption(manifest, "cdrFlipAxes", FlipAxes, "axis", axis);

        VerbGate.WarnIfUnavailable("shape.flip", corelVersion);
        ComInvoker.Call(ShapeTargets.At(app, index), "Flip", (int)axes);
        Console.WriteLine($"Flipped shape {index} ({axis}).");
        return 0;
    }

    public static int Duplicate(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape duplicate --index N [--offset-x X] [--offset-y Y] [--unit U]";
        if (!ShapeCommands.TryIndex(reader, out int index))
        {
            return ShapeCommands.Usage(usage);
        }
        double offsetX = reader.DoubleOrNull("offset-x") ?? 0;
        double offsetY = reader.DoubleOrNull("offset-y") ?? 0;

        ShapeCommands.ApplyUnit(app, reader, corelVersion);
        VerbGate.WarnIfUnavailable("shape.duplicate", corelVersion);
        ComInvoker.Call(ShapeTargets.At(app, index), "Duplicate", offsetX, offsetY);
        Console.WriteLine(
            $"Duplicated shape {index} at offset ({offsetX},{offsetY}). Indexes have shifted - re-run `shape list`.");
        return 0;
    }

    public static int Order(object app, ArgReader reader, string? corelVersion)
    {
        const string usage = "corel shape order --index N --to front|back|forward|backward";
        string? to = reader.Flag("to");
        if (!ShapeCommands.TryIndex(reader, out int index)
            || to is null
            || !OrderMethods.TryGetValue(to, out string? method))
        {
            return ShapeCommands.Usage(usage);
        }

        VerbGate.WarnIfUnavailable("shape.order", corelVersion);
        ComInvoker.Call(ShapeTargets.At(app, index), method);
        Console.WriteLine($"Moved shape {index} {to} in the z-order. Indexes have shifted - re-run `shape list`.");
        return 0;
    }
}
