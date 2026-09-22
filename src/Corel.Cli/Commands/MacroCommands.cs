using Corel.Automation;

namespace Corel.Cli.Commands;

/// <summary>
/// `corel macro install --name &lt;Module&gt; --file &lt;path.bas&gt;` injects VBA source into the
/// ACTIVE DOCUMENT'S OWN VBA project - not global macro storage (GMS) - so it travels
/// with the .cdr file.
/// </summary>
/// <remarks>
/// There is deliberately no `macro run`: CorelDRAW's COM automation model has no way to
/// invoke document-embedded VBA code from outside the app. The only run mechanism found
/// (Application.GMSManager.RunMacro / IVGGMSMacro.Run) is scoped to GMS project macros
/// (loaded .gms files) and returns "Macro module not found" for anything in a document's
/// own VBAProject, confirmed empirically. There is also no hidden/undocumented
/// Application.Run (Excel has one; CorelDRAW does not - DISP_E_UNKNOWNNAME). Installed
/// macros are meant to be run by the user inside CorelDRAW (Alt+F8, or bound to a
/// toolbar/shortcut) - which matches the original workflow this was designed for anyway.
/// </remarks>
public static class MacroCommands
{
    public static int Run(string[] args, object app, string? corelVersion)
    {
        if (args.Length == 0 || args[0] != "install")
        {
            Console.Error.WriteLine("Usage: corel macro install --name <ModuleName> --file <path.bas>");
            return 1;
        }

        var reader = new ArgReader(args[1..]);
        string? moduleName = reader.Flag("name");
        string? filePath = reader.Flag("file");
        if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(filePath))
        {
            Console.Error.WriteLine("Usage: corel macro install --name <ModuleName> --file <path.bas>");
            return 1;
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Source file not found: {filePath}");
            return 1;
        }

        VerbGate.WarnIfUnavailable("macro.install", corelVersion);
        ComInvoker.Call(app, "InitializeVBA");
        object doc = ComInvoker.ResolvePath(app, "ActiveDocument")!;
        int docIndex = Convert.ToInt32(ComInvoker.Get(doc, "Index"));

        object project = FindDocumentVbProject(app, docIndex)
            ?? throw new InvalidOperationException(
                "Could not find a VBA project for the active document. Is a document open?");

        object components = ComInvoker.Get(project, "VBComponents")!;
        RemoveIfExists(components, moduleName);

        object newComponent = ComInvoker.Call(components, "Add", 1)!; // vbext_ct_StdModule
        ComInvoker.Set(newComponent, "Name", moduleName);

        object codeModule = ComInvoker.Get(newComponent, "CodeModule")!;
        ComInvoker.Call(codeModule, "AddFromString", File.ReadAllText(filePath));

        Console.WriteLine($"Installed module '{moduleName}' into the active document's VBA project.");
        Console.WriteLine("Run it from inside CorelDRAW (Alt+F8, or bind it to a toolbar/shortcut) -");
        Console.WriteLine("there is no automation API to invoke a document-embedded macro externally.");
        return 0;
    }

    /// <summary>
    /// Every document gets its own "VBAProject"-named entry in VBE.VBProjects (alongside
    /// GlobalMacros and any loaded .gms projects) - since the name doesn't disambiguate
    /// between open documents, match via ThisDocument's Properties extender, which
    /// mirrors the real Document object's own properties (including Index) exactly.
    /// </summary>
    private static object? FindDocumentVbProject(object app, int docIndex)
    {
        object vbe = ComInvoker.Get(app, "VBE")!;
        object vbProjects = ComInvoker.Get(vbe, "VBProjects")!;
        int count = Convert.ToInt32(ComInvoker.Get(vbProjects, "Count"));

        for (int i = 1; i <= count; i++)
        {
            object candidate = ComInvoker.Get(vbProjects, "Item", i)!;
            if ((string)ComInvoker.Get(candidate, "Name")! != "VBAProject")
            {
                continue;
            }

            object components = ComInvoker.Get(candidate, "VBComponents")!;
            object thisDocument = ComInvoker.Get(components, "Item", "ThisDocument")!;
            object properties = ComInvoker.Get(thisDocument, "Properties")!;
            object indexProperty = ComInvoker.Get(properties, "Item", "Index")!;
            int candidateIndex = Convert.ToInt32(ComInvoker.Get(indexProperty, "Value"));

            if (candidateIndex == docIndex)
            {
                return candidate;
            }
        }

        return null;
    }

    private static void RemoveIfExists(object components, string name)
    {
        try
        {
            object existing = ComInvoker.Get(components, "Item", name)!;
            ComInvoker.Call(components, "Remove", existing);
        }
        catch
        {
            // didn't exist - nothing to remove
        }
    }
}
