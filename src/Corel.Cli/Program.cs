using Corel.Automation;
using Corel.Cli;
using Corel.Schema;

if (args.Length == 0)
{
    Console.WriteLine("Usage: corel <command> [args...]");
    Console.WriteLine("Commands: detect, schema generate, raw <get|set|call>, page size,");
    Console.WriteLine("          shape <create-rect|list|delete>, docs <list|use|active>, shell");
    return 1;
}

(string[] rest, string? corelVersion) = ExtractGlobalFlag(args, "--corel-version");

using var host = new StaThreadHost();
try
{
    return host.Invoke(() => Run(rest, corelVersion));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static int Run(string[] args, string? corelVersion)
{
    if (args[0] == "shell")
    {
        (object shellApp, string? shellVersion) = Connect(corelVersion);
        return Repl.Run(shellApp, shellVersion);
    }

    return CommandDispatcher.Dispatch(args, () => Connect(corelVersion));
}

static (object App, string? Version) Connect(string? corelVersion)
{
    InstallRecord? record = InstallStore.Load();
    string progId = ComAttach.DefaultProgId;
    string? resolvedVersion = null;
    if (record is not null)
    {
        CorelInstall install = CorelVersionResolver.Resolve(record, corelVersion);
        progId = install.ProgId;
        resolvedVersion = install.Version;
    }
    return (ComAttach.Attach(progId), resolvedVersion);
}

static (string[] Remaining, string? Value) ExtractGlobalFlag(string[] args, string flagName)
{
    var rest = new List<string>();
    string? value = null;
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == flagName && i + 1 < args.Length)
        {
            value = args[++i];
        }
        else
        {
            rest.Add(args[i]);
        }
    }
    return ([.. rest], value);
}
