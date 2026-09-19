using Corel.Automation;
using Corel.Cli;
using Corel.Schema;

if (args.Length == 0)
{
    Console.WriteLine("Usage: corel <command> [args...]");
    Console.WriteLine("Commands: detect, schema generate, raw <get|set|call>, page size,");
    Console.WriteLine("          shape <create-rect|list|delete>, docs <list|use|active>,");
    Console.WriteLine("          doc <new|save [--as path]>, macro install --name N --file F.bas,");
    Console.WriteLine("          script run <file.cdx> [--force], shell");
    return 1;
}

(string[] afterVersion, string? corelVersion) = ExtractGlobalFlag(args, "--corel-version");
(string[] rest, bool force) = ExtractBoolFlag(afterVersion, "--force");

using var host = new StaThreadHost();
try
{
    return host.Invoke(() => Run(rest, corelVersion, force));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ErrorMessages.Friendly(ex)}");
    return 1;
}

static int Run(string[] args, string? corelVersion, bool force)
{
    if (args[0] == "shell")
    {
        (object shellApp, string? shellVersion) = Connect(corelVersion);
        return Repl.Run(shellApp, shellVersion);
    }

    if (args[0] == "script" && args.Length > 1 && args[1] == "run")
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: corel script run <file.cdx> [--force]");
            return 1;
        }
        (object scriptApp, string? scriptVersion) = Connect(corelVersion);
        return ScriptRunner.Run(args[2], scriptApp, scriptVersion, force);
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

static (string[] Remaining, bool Present) ExtractBoolFlag(string[] args, string flagName)
{
    var rest = new List<string>();
    bool present = false;
    foreach (string arg in args)
    {
        if (arg == flagName)
        {
            present = true;
        }
        else
        {
            rest.Add(arg);
        }
    }
    return ([.. rest], present);
}
