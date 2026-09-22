using Corel.Automation;

namespace Corel.Cli.Commands;

public static class DocCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel doc <new|save>");
            return 1;
        }

        return args[0] switch
        {
            "new" => New(app),
            "save" => Save(app, args[1..], corelVersion),
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

        if (path is null)
        {
            ComInvoker.Call(doc, "Save");
            Console.WriteLine("Saved.");
            return 0;
        }

        object options = ComInvoker.Call(app, "CreateStructSaveAsOptions")!;
        ComInvoker.Set(options, "Overwrite", true);
        // Only request VBA embedding when actually needed - it's an opt-in, not the
        // default, regardless of any interaction with the interactive "Save Drawing"
        // dialog bug tracked separately (see the task board): CreateStructSaveAsOptions
        // defaults EmbedVBAProject to true on its own, so this is a deliberate override,
        // not a workaround for that bug.
        if (reader.Flag("embed-vba") is not null)
        {
            ComInvoker.Call(app, "InitializeVBA");
            ComInvoker.Set(options, "EmbedVBAProject", true);
        }
        SaveDialogWatchdog.RunWithAutoDismiss(() => ComInvoker.Call(doc, "SaveAs", path, options));
        Console.WriteLine($"Saved to {path}");
        return 0;
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown doc subcommand '{sub}'.");
        return 1;
    }
}
