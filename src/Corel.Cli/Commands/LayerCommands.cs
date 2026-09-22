using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>
/// Layers live on the active page (ActivePage.Layers). Confirmed against the real
/// generated capability manifest: IVGLayers has NO First/Last, only Item(IndexOrName),
/// Count, Find(LayerName) and Top/Bottom - so lookups here use Item/Find rather than
/// the first/next walk DocsCommands uses for IVGDocuments.
/// </summary>
public static class LayerCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel layer <list|add|use|delete> ...");
            return 1;
        }

        string sub = args[0];
        var reader = new ArgReader(args[1..]);

        return sub switch
        {
            "list" => List(app, corelVersion),
            "add" => Add(app, reader, corelVersion),
            "use" => Use(app, reader, corelVersion),
            "delete" => Delete(app, reader, corelVersion),
            _ => Unknown(sub),
        };
    }

    private static int List(object app, string? corelVersion)
    {
        VerbGate.WarnIfUnavailable("layer.list", corelVersion);
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        object page = ComInvoker.Get(doc, "ActivePage")!;
        object layers = ComInvoker.Get(page, "Layers")!;
        int count = Convert.ToInt32(ComInvoker.Get(layers, "Count"));

        object? activeLayer = ComInvoker.Get(doc, "ActiveLayer");
        int? activeIndex = activeLayer is null ? null : Convert.ToInt32(ComInvoker.Get(activeLayer, "Index"));

        for (int i = 1; i <= count; i++)
        {
            object layer = ComInvoker.Get(layers, "Item", i)!;
            string name = ComInvoker.Get(layer, "Name")?.ToString() ?? "";
            string marker = i == activeIndex ? "  (active)" : "";
            Console.WriteLine($"  [{i}] {name}{marker}");
        }
        return 0;
    }

    private static int Add(object app, ArgReader reader, string? corelVersion)
    {
        string? name = reader.Flag("name");
        if (string.IsNullOrEmpty(name))
        {
            Console.Error.WriteLine("Usage: corel layer add --name <name>");
            return 1;
        }

        VerbGate.WarnIfUnavailable("layer.add", corelVersion);
        object page = ComInvoker.ResolvePath(app, "ActiveDocument.ActivePage")!;
        ComInvoker.Call(page, "CreateLayer", name);
        Console.WriteLine($"Created layer '{name}'.");
        return 0;
    }

    private static int Use(object app, ArgReader reader, string? corelVersion)
    {
        object? layer = ResolveSelector(app, reader, "use", out int errorCode);
        if (layer is null)
        {
            return errorCode;
        }

        VerbGate.WarnIfUnavailable("layer.use", corelVersion);
        ComInvoker.Call(layer, "Activate");
        Console.WriteLine($"Using layer '{ComInvoker.Get(layer, "Name")}'.");
        return 0;
    }

    private static int Delete(object app, ArgReader reader, string? corelVersion)
    {
        object? layer = ResolveSelector(app, reader, "delete", out int errorCode);
        if (layer is null)
        {
            return errorCode;
        }

        string name = ComInvoker.Get(layer, "Name")?.ToString() ?? "";
        VerbGate.WarnIfUnavailable("layer.delete", corelVersion);
        ComInvoker.Call(layer, "Delete");
        Console.WriteLine($"Deleted layer '{name}'.");
        return 0;
    }

    /// <summary>--name and --index are an OR, not a required-together pair, which
    /// CommandGrammar's RequiredTogether can't express - enforced here instead, same as
    /// every other "at least one of" case in this codebase (e.g. docs use's single
    /// name-or-index positional).</summary>
    private static object? ResolveSelector(object app, ArgReader reader, string action, out int errorCode)
    {
        string? name = reader.Flag("name");
        string? indexArg = reader.Flag("index");

        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(indexArg))
        {
            Console.Error.WriteLine($"Usage: corel layer {action} (--name <name> | --index N)");
            errorCode = 1;
            return null;
        }

        object page = ComInvoker.ResolvePath(app, "ActiveDocument.ActivePage")!;
        object layers = ComInvoker.Get(page, "Layers")!;

        if (!string.IsNullOrEmpty(indexArg))
        {
            if (!int.TryParse(indexArg, out int index))
            {
                Console.Error.WriteLine($"Usage: corel layer {action} --index N (N must be a whole number)");
                errorCode = 1;
                return null;
            }
            int count = Convert.ToInt32(ComInvoker.Get(layers, "Count"));
            if (index < 1 || index > count)
            {
                Console.Error.WriteLine($"No layer at index {index}. Use `corel layer list` to see layers (1-{count}).");
                errorCode = 1;
                return null;
            }
            errorCode = 0;
            return ComInvoker.Get(layers, "Item", index);
        }

        object? found = ComInvoker.Call(layers, "Find", name);
        if (found is null)
        {
            Console.Error.WriteLine($"No layer named '{name}'. Use `corel layer list` to see layers.");
            errorCode = 1;
            return null;
        }
        errorCode = 0;
        return found;
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown layer subcommand '{sub}'.");
        return 1;
    }
}
