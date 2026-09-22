using Corel.Automation;

namespace Corel.Cli.Commands;

public static class ImageCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine(Usage);
            return 1;
        }

        return args[0] switch
        {
            "import" => Import(app, new ArgReader(args[1..]), corelVersion),
            _ => Unknown(args[0]),
        };
    }

    private const string Usage =
        "Usage: corel image import --file <path> (--into <shapeName> | --left L --top T --right R --bottom B) [--unit U]";

    private static int Import(object app, ArgReader reader, string? corelVersion)
    {
        string? file = reader.Flag("file");
        if (file is null)
        {
            Console.Error.WriteLine(Usage);
            return 1;
        }

        file = Path.GetFullPath(file);
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"File not found: {file}");
            return 1;
        }

        int filter;
        try
        {
            filter = FilterForExtension(file);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        if (reader.Flag("unit") is string unitName)
        {
            ComInvoker.Set(doc, "Unit", UnitNames.ToEnumValue(unitName));
        }
        object layer = ComInvoker.Get(doc, "ActiveLayer")!;

        string? intoName = reader.Flag("into");
        object? container = null;
        double left, top, right, bottom;
        if (intoName is not null)
        {
            container = FindShapeByName(layer, intoName)
                ?? throw new InvalidOperationException($"No shape named '{intoName}' on the active layer.");
            // Read the container's own bounds directly - already in the document's
            // current unit, so no conversion is needed to line the import up with it.
            left = Convert.ToDouble(ComInvoker.Get(container, "LeftX"));
            top = Convert.ToDouble(ComInvoker.Get(container, "TopY"));
            right = Convert.ToDouble(ComInvoker.Get(container, "RightX"));
            bottom = Convert.ToDouble(ComInvoker.Get(container, "BottomY"));
        }
        else
        {
            double? l = reader.DoubleOrNull("left");
            double? t = reader.DoubleOrNull("top");
            double? r = reader.DoubleOrNull("right");
            double? b = reader.DoubleOrNull("bottom");
            if (l is null || t is null || r is null || b is null)
            {
                Console.Error.WriteLine(Usage);
                return 1;
            }
            (left, top, right, bottom) = (l.Value, t.Value, r.Value, b.Value);
        }

        VerbGate.WarnIfUnavailable("image.import", corelVersion);

        // Layer.Import is a VBA Sub - it has no return value. The imported shape is
        // selected as a side effect instead (per the SDK docs), so it's read back via
        // Document.Selection. Options can't be omitted via Type.Missing here either -
        // unlike most trailing-optional COM params, it's a raw struct pointer that only
        // accepts a real instance (confirmed empirically: Missing throws E_FAIL).
        object importOptions = ComInvoker.Call(app, "CreateStructImportOptions")!;
        ComInvoker.Call(layer, "Import", file, filter, importOptions);
        object selectionShapes = ComInvoker.Get(ComInvoker.Get(doc, "Selection")!, "Shapes")!;
        object shape = ComInvoker.Get(selectionShapes, "Item", 1)!;

        ComInvoker.Call(
            shape, "SetBoundingBox", left, bottom, right - left, top - bottom, false, (int)ReferencePoint.BottomLeft);

        if (container is not null)
        {
            // The imported shape (not the container) is the caller here - "adds this
            // shape to a PowerClip object" per the SDK docs. CenterInContainer is a
            // cdrTriState, not a bool; cdrFalse since bounds were already set exactly.
            ComInvoker.Call(shape, "AddToPowerClip", container, (int)TriState.False);
            Console.WriteLine($"Imported '{Path.GetFileName(file)}' and power-clipped into '{intoName}'.");
        }
        else
        {
            Console.WriteLine($"Imported '{Path.GetFileName(file)}' at ({left},{bottom})-({right},{top}).");
        }
        return 0;
    }

    private static object? FindShapeByName(object layer, string name)
    {
        object shapes = ComInvoker.Get(layer, "Shapes")!;
        int count = Convert.ToInt32(ComInvoker.Get(shapes, "Count"));
        for (int i = 1; i <= count; i++)
        {
            object shape = ComInvoker.Get(shapes, "Item", i)!;
            if (string.Equals(Convert.ToString(ComInvoker.Get(shape, "Name")), name, StringComparison.Ordinal))
            {
                return shape;
            }
        }
        return null;
    }

    // cdrFilter values, ground-truth from the scanned type library (Corel.Schema).
    internal static int FilterForExtension(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".png" => 802,
        ".jpg" or ".jpeg" => 774,
        ".tif" or ".tiff" => 772,
        ".bmp" => 769,
        _ => throw new ArgumentException($"Unsupported image extension for import: '{file}'."),
    };

    // cdrReferencePoint values, ground-truth from the scanned type library.
    private enum ReferencePoint
    {
        BottomLeft = 5,
    }

    // cdrTriState values, ground-truth from the scanned type library.
    private enum TriState
    {
        False = 0,
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown image subcommand '{sub}'.");
        return 1;
    }
}
