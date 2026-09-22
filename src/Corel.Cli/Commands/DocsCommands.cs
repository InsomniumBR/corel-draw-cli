using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>
/// "Instance selection" only ever means picking an open document within the one
/// automatable CorelDRAW process - CorelDRAW never registers in the ROT, and a second
/// process is entirely COM-invisible, so there is no such thing as selecting between
/// separate OS processes here (confirmed empirically). `docs use` calls the real
/// Document.Activate() so every other command's "ActiveDocument" path resolves correctly
/// afterward, rather than tracking a separate local pointer.
/// </summary>
public static class DocsCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel docs <list|use|active>");
            return 1;
        }

        return args[0] switch
        {
            "list" => List(app, corelVersion),
            "use" => Use(app, args[1..]),
            "active" => Active(app, corelVersion),
            _ => Unknown(args[0]),
        };
    }

    private static int List(object app, string? corelVersion)
    {
        VerbGate.WarnIfUnavailable("docs.list", corelVersion);
        object documents = ComInvoker.Get(app, "Documents")!;
        int count = Convert.ToInt32(ComInvoker.Get(documents, "Count"));
        if (count == 0)
        {
            Console.WriteLine("No documents open.");
            return 0;
        }

        object? activeDoc = ComInvoker.Get(app, "ActiveDocument");
        int? activeIndex = activeDoc is null ? null : Convert.ToInt32(ComInvoker.Get(activeDoc, "Index"));

        for (int i = 1; i <= count; i++)
        {
            object doc = ComInvoker.Get(documents, "Item", i)!;
            string name = (string)ComInvoker.Get(doc, "Name")!;
            string marker = i == activeIndex ? "  (active)" : "";
            Console.WriteLine($"  [{i}] {name}{marker}");
        }
        return 0;
    }

    /// <summary>Accepts a name or a `docs list` index; an ambiguous name errors rather than picking one silently.</summary>
    private static int Use(object app, string[] rest)
    {
        if (rest.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel docs use <name|index>");
            return 1;
        }
        string selector = rest[0];

        object documents = ComInvoker.Get(app, "Documents")!;
        int count = Convert.ToInt32(ComInvoker.Get(documents, "Count"));

        if (int.TryParse(selector, out int index))
        {
            if (index < 1 || index > count)
            {
                Console.Error.WriteLine($"No document at index {index}. Use `docs list` to see open documents (1-{count}).");
                return 1;
            }
            object indexed = ComInvoker.Get(documents, "Item", index)!;
            string indexedName = (string)ComInvoker.Get(indexed, "Name")!;
            ComInvoker.Call(indexed, "Activate");
            Console.WriteLine($"Using document '{indexedName}' (index {index}).");
            return 0;
        }

        List<int> matches = [];
        for (int i = 1; i <= count; i++)
        {
            object doc = ComInvoker.Get(documents, "Item", i)!;
            if ((string)ComInvoker.Get(doc, "Name")! == selector)
            {
                matches.Add(i);
            }
        }

        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"No open document named '{selector}'. Use `docs list` to see open documents.");
            return 1;
        }
        if (matches.Count > 1)
        {
            Console.Error.WriteLine(
                $"Ambiguous: {matches.Count} open documents are named '{selector}' (indexes {string.Join(", ", matches)}). " +
                "Use `docs use <index>` to disambiguate.");
            return 1;
        }

        object target = ComInvoker.Get(documents, "Item", matches[0])!;
        ComInvoker.Call(target, "Activate");
        Console.WriteLine($"Using document '{selector}'.");
        return 0;
    }

    private static int Active(object app, string? corelVersion)
    {
        VerbGate.WarnIfUnavailable("docs.active", corelVersion);
        object? activeDoc = ComInvoker.Get(app, "ActiveDocument");
        if (activeDoc is null)
        {
            Console.WriteLine("No document is active.");
            return 0;
        }
        Console.WriteLine((string)ComInvoker.Get(activeDoc, "Name")!);
        return 0;
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown docs subcommand '{sub}'.");
        return 1;
    }
}
