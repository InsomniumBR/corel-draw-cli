using Corel.Automation;
using Corel.Cli;
using Corel.Schema;

// Running `corel` bare is the documented install check, so it is a successful
// "show usage", not an error.
if (args.Length == 0)
{
    Console.WriteLine(HelpText.Overview());
    return 0;
}

(string[] rest, string? corelVersion) = ExtractLeadingGlobals(args);
if (rest.Length == 0)
{
    Console.WriteLine(HelpText.Overview());
    return 0;
}

using var host = new StaThreadHost();
try
{
    return host.Invoke(() => Run(rest, corelVersion));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ErrorMessages.Friendly(ex)}");
    return 1;
}

static int Run(string[] args, string? corelVersion)
{
    CorelSession session = CreateSession(corelVersion);
    return args[0] == "shell"
        ? Repl.Run(session)
        : CommandDispatcher.Dispatch(args, session);
}

static CorelSession CreateSession(string? corelVersion)
{
    InstallRecord? record = InstallStore.Load();
    if (record is null)
    {
        return new CorelSession(null, () => ComAttach.Attach(ComAttach.DefaultProgId));
    }

    CorelInstall install = CorelVersionResolver.Resolve(record, corelVersion);
    return new CorelSession(install.Version, () => ComAttach.Attach(install.ProgId));
}

// Only the leading run of arguments is scanned, so a command's own --force/--vars-shaped
// flag can never be mistaken for a global one.
static (string[], string?) ExtractLeadingGlobals(string[] args)
{
    string? version = null;
    int i = 0;
    while (i + 1 < args.Length && args[i] == "--corel-version")
    {
        version = args[i + 1];
        i += 2;
    }
    return (args[i..], version);
}
