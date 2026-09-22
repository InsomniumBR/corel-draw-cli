using Corel.Automation;

namespace Corel.Cli.Commands;

public static class PageCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel page <size|add|delete|goto|list> ...");
            return 1;
        }

        string sub = args[0];
        var reader = new ArgReader(args[1..]);

        return sub switch
        {
            "size" => Size(app, reader, corelVersion),
            "add" => Add(app, reader, corelVersion),
            "delete" => Delete(app, reader, corelVersion),
            "goto" => Goto(app, reader, corelVersion),
            "list" => List(app, corelVersion),
            _ => Unknown(sub),
        };
    }

    private static int Size(object app, ArgReader reader, string? corelVersion)
    {
        string? unitName = reader.Flag("unit");
        int? unitValue = unitName is not null ? UnitNames.ToEnumValue(unitName) : null;

        double? width = reader.DoubleOrNull("width");
        double? height = reader.DoubleOrNull("height");

        if (width is null != height is null)
        {
            Console.Error.WriteLine(
                "Usage: corel page size --width W --height H [--unit in|mm|...] (--width and --height must be given together)");
            return 1;
        }

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object page = ComInvoker.Get(doc, "ActivePage")!;

        if (unitValue is not null)
        {
            VerbGate.WarnIfUnavailable("doc.unit.set", corelVersion);
            ComInvoker.Set(doc, "Unit", unitValue.Value);
        }

        if (width is not null && height is not null)
        {
            VerbGate.WarnIfUnavailable("page.size.set", corelVersion);
            ComInvoker.Call(page, "SetSize", width.Value, height.Value);
            Console.WriteLine($"Page size set to {width}x{height} ({unitName ?? "current unit"}).");
        }
        else
        {
            VerbGate.WarnIfUnavailable("page.size.get", corelVersion);
            object? w = ComInvoker.Get(page, "SizeWidth");
            object? h = ComInvoker.Get(page, "SizeHeight");
            Console.WriteLine($"{w} x {h}");
        }

        return 0;
    }

    /// <summary>Live-verified signature: Document.InsertPages(NumberOfPages, BeforePage: bool, Page:
    /// int) - "Page" is the reference page index, "BeforePage" picks which side of it to insert on.
    /// Not guessed from the manifest's bare parameter names alone (Page's real meaning wasn't obvious
    /// from "NumberOfPages, BeforePage, Page" until tried live against a real document).</summary>
    private static int Add(object app, ArgReader reader, string? corelVersion)
    {
        string? afterArg = reader.Flag("after");
        int? after = null;
        if (!string.IsNullOrEmpty(afterArg))
        {
            if (!int.TryParse(afterArg, out int parsed))
            {
                Console.Error.WriteLine("Usage: corel page add [--after N] (N must be a whole number)");
                return 1;
            }
            after = parsed;
        }

        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;

        if (after is null)
        {
            VerbGate.WarnIfUnavailable("page.add", corelVersion);
            ComInvoker.Call(doc, "AddPages", 1);
            Console.WriteLine("Added a page at the end.");
        }
        else
        {
            VerbGate.WarnIfUnavailable("page.insert", corelVersion);
            ComInvoker.Call(doc, "InsertPages", 1, false, after.Value);
            Console.WriteLine($"Added a page after page {after}.");
        }

        return 0;
    }

    private static int Delete(object app, ArgReader reader, string? corelVersion)
    {
        string? indexArg = reader.Flag("index");
        if (string.IsNullOrEmpty(indexArg) || !int.TryParse(indexArg, out int index))
        {
            Console.Error.WriteLine("Usage: corel page delete --index N (1-based, see `corel page list`)");
            return 1;
        }

        VerbGate.WarnIfUnavailable("page.delete", corelVersion);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object pages = ComInvoker.Get(doc, "Pages")!;
        object page = ComInvoker.Get(pages, "Item", index)!;
        ComInvoker.Call(page, "Delete");
        Console.WriteLine(
            $"Deleted page at index {index}. Remaining pages have renumbered - re-run `page list` " +
            "before deleting another by index.");
        return 0;
    }

    private static int Goto(object app, ArgReader reader, string? corelVersion)
    {
        string? indexArg = reader.Flag("index");
        if (string.IsNullOrEmpty(indexArg) || !int.TryParse(indexArg, out int index))
        {
            Console.Error.WriteLine("Usage: corel page goto --index N (1-based, see `corel page list`)");
            return 1;
        }

        VerbGate.WarnIfUnavailable("page.goto", corelVersion);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object pages = ComInvoker.Get(doc, "Pages")!;
        object page = ComInvoker.Get(pages, "Item", index)!;
        ComInvoker.Call(page, "Activate");
        string? name = ComInvoker.Get(page, "Name")?.ToString();
        Console.WriteLine($"Now on page {index}{(name is null ? "" : $" ('{name}')")}.");
        return 0;
    }

    private static int List(object app, string? corelVersion)
    {
        VerbGate.WarnIfUnavailable("page.list", corelVersion);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object pages = ComInvoker.Get(doc, "Pages")!;
        int count = Convert.ToInt32(ComInvoker.Get(pages, "Count"));

        object? activePage = ComInvoker.Get(doc, "ActivePage");
        int? activeIndex = activePage is null ? null : Convert.ToInt32(ComInvoker.Get(activePage, "Index"));

        for (int i = 1; i <= count; i++)
        {
            object page = ComInvoker.Get(pages, "Item", i)!;
            string name = ComInvoker.Get(page, "Name")?.ToString() ?? "";
            string marker = i == activeIndex ? "  (active)" : "";
            Console.WriteLine($"  [{i}] {name}{marker}");
        }
        return 0;
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown page subcommand '{sub}'.");
        return 1;
    }
}
