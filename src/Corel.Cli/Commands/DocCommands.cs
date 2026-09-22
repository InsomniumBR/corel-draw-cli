using Corel.Automation;

namespace Corel.Cli.Commands;

public static class DocCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel doc <new|save|export|undo|redo>");
            return 1;
        }

        return args[0] switch
        {
            "new" => New(app),
            "save" => Save(app, args[1..], corelVersion),
            "export" => Export(app, new ArgReader(args[1..]), corelVersion),
            "undo" => Undo(app, new ArgReader(args[1..]), corelVersion),
            "redo" => Redo(app, new ArgReader(args[1..]), corelVersion),
            _ => Unknown(args[0]),
        };
    }

    private static int New(object app)
    {
        object doc = ComInvoker.Call(app, "CreateDocument")!;
        Console.WriteLine($"Created document '{ComInvoker.Get(doc, "Name")}'.");
        return 0;
    }

    private static int Save(object app, string[] args, string? corelVersion)
    {
        VerbGate.WarnIfUnavailable("doc.save", corelVersion);
        var reader = new ArgReader(args);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        string? path = reader.Flag("as");

        if (string.IsNullOrEmpty(path))
        {
            ComInvoker.Call(doc, "Save");
            Console.WriteLine("Saved.");
            return 0;
        }

        string? targetDir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Console.Error.WriteLine($"Target directory does not exist: {targetDir}");
            return 1;
        }

        object options = ComInvoker.Call(app, "CreateStructSaveAsOptions")!;
        ComInvoker.Set(options, "Overwrite", true);
        // --embed-vba is opt-in: CreateStructSaveAsOptions defaults EmbedVBAProject to true on its own.
        if (reader.Flag("embed-vba") is not null)
        {
            ComInvoker.Call(app, "InitializeVBA");
            ComInvoker.Set(options, "EmbedVBAProject", true);
        }

        void SaveAsAction() => ComInvoker.Call(doc, "SaveAs", path, options);

        // Resolving which process to watch is isolated from the save itself: a failure
        // here only disables the watchdog (a defense-in-depth safety net), it must
        // never be confused with - or swallow - a real failure from SaveAs below.
        uint? corelProcessId;
        try
        {
            corelProcessId = SaveDialogWatchdog.ResolveCorelProcessId(app);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: could not resolve CorelDRAW's process for the save-dialog watchdog ({ex.Message}); saving without it.");
            corelProcessId = null;
        }

        if (corelProcessId is uint pid)
        {
            SaveDialogWatchdog.RunWithAutoDismiss(pid, SaveAsAction);
        }
        else
        {
            SaveAsAction();
        }

        Console.WriteLine($"Saved to {path}");
        return 0;
    }

    private const string ExportUsage =
        "Usage: corel doc export --as <path> --format png|jpg|svg|pdf [--width W --height H] [--dpi D]";

    private static int Export(object app, ArgReader reader, string? corelVersion)
    {
        string? path = reader.Flag("as");
        string? format = reader.Flag("format")?.ToLowerInvariant();
        double? width = reader.DoubleOrNull("width");
        double? height = reader.DoubleOrNull("height");
        double? dpi = reader.DoubleOrNull("dpi");

        if (string.IsNullOrEmpty(path) || format is null)
        {
            Console.Error.WriteLine(ExportUsage);
            return 1;
        }

        if (format is not ("png" or "jpg" or "jpeg" or "svg" or "pdf"))
        {
            Console.Error.WriteLine($"Unsupported export format '{format}'. Supported: png, jpg, svg, pdf.");
            return 1;
        }

        path = Path.GetFullPath(path);
        string? targetDir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Console.Error.WriteLine($"Target directory does not exist: {targetDir}");
            return 1;
        }

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;

        // pdf uses the already-proven PublishToPDF call directly rather than the generic Export path below.
        if (format == "pdf")
        {
            VerbGate.WarnIfUnavailable("doc.export.pdf", corelVersion);
            ComInvoker.Call(doc, "PublishToPDF", path);
            Console.WriteLine($"Exported to {path}");
            return 0;
        }

        VerbGate.WarnIfUnavailable("doc.export", corelVersion);
        int filter = FilterForFormat(format);

        // Export's Options/PaletteOptions are structs, not scalars - Type.Missing is rejected, same as image import.
        object options = ComInvoker.Call(app, "CreateStructExportOptions")!;
        ComInvoker.Set(options, "Overwrite", true);
        ComInvoker.Set(options, "ResolutionX", dpi ?? 300d);
        ComInvoker.Set(options, "ResolutionY", dpi ?? 300d);
        ComInvoker.Set(options, "ImageType", (int)ImageType.Rgb);
        ComInvoker.Set(options, "AntiAliasingType", (int)AntiAliasingType.Normal);
        ComInvoker.Set(options, "Compression", (int)CompressionType.None);
        ComInvoker.Set(options, "MaintainLayers", false);
        // Verified live: Export always crops to the drawing's object extent, not the page - see AGENTS.md.
        ComInvoker.Set(options, "MaintainAspect", true);
        ComInvoker.Set(options, "Transparent", false);
        ComInvoker.Set(options, "Dithered", false);
        ComInvoker.Set(options, "UseColorProfile", false);
        if (width is not null)
        {
            ComInvoker.Set(options, "SizeX", width.Value);
        }
        if (height is not null)
        {
            ComInvoker.Set(options, "SizeY", height.Value);
        }

        object paletteOptions = ComInvoker.Call(app, "CreateStructPaletteOptions")!;

        ComInvoker.Call(doc, "Export", path, filter, (int)ExportRange.AllPages, options, paletteOptions);

        Console.WriteLine($"Exported to {path}");
        return 0;
    }

    // cdrFilter values, ground-truth from the scanned type library (Corel.Schema).
    internal static int FilterForFormat(string format) => format switch
    {
        "png" => 802,
        "jpg" or "jpeg" => 774,
        "svg" => 1345,
        _ => throw new ArgumentException($"Unsupported export format '{format}'. Supported: png, jpg, svg, pdf."),
    };

    // cdrImageType values, ground-truth from the scanned type library.
    private enum ImageType
    {
        Rgb = 4,
    }

    // cdrAntiAliasingType values, ground-truth from the scanned type library.
    private enum AntiAliasingType
    {
        Normal = 1,
    }

    // cdrCompressionType values, ground-truth from the scanned type library.
    private enum CompressionType
    {
        None = 0,
    }

    // cdrExportRange values, ground-truth from the scanned type library.
    private enum ExportRange
    {
        AllPages = 0,
    }

    private static int Undo(object app, ArgReader reader, string? corelVersion)
    {
        int levels = LevelsOrDefault(reader);
        VerbGate.WarnIfUnavailable("doc.undo", corelVersion);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        ComInvoker.Call(doc, "Undo", levels);
        Console.WriteLine($"Undid {levels} step(s).");
        return 0;
    }

    private static int Redo(object app, ArgReader reader, string? corelVersion)
    {
        int levels = LevelsOrDefault(reader);
        VerbGate.WarnIfUnavailable("doc.redo", corelVersion);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        ComInvoker.Call(doc, "Redo", levels);
        Console.WriteLine($"Redid {levels} step(s).");
        return 0;
    }

    private static int LevelsOrDefault(ArgReader reader) => (int)(reader.DoubleOrNull("levels") ?? 1);

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown doc subcommand '{sub}'.");
        return 1;
    }
}
