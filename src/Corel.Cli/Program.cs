using Corel.Automation;
using Corel.Cli;
using Corel.Cli.Commands;
using Corel.Schema;

if (args.Length == 0)
{
    Console.WriteLine("Usage: corel <command> [args...]");
    Console.WriteLine("Commands: detect, schema generate, raw <get|set|call>, page size, shape <create-rect|list|delete>");
    return 1;
}

(string[] rest, string? corelVersion) = ExtractGlobalFlag(args, "--corel-version");

using var host = new StaThreadHost();
try
{
    return host.Invoke(() => Dispatch(rest, corelVersion));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static int Dispatch(string[] args, string? corelVersion)
{
    string command = args[0];
    string[] commandArgs = args[1..];

    // These two don't need a live COM attach at all.
    if (command == "detect")
    {
        return DetectCommand.Run(commandArgs);
    }
    if (command == "schema")
    {
        return SchemaCommands.Run(commandArgs);
    }

    (object app, string? resolvedVersion) = Connect(corelVersion);
    return command switch
    {
        "raw" => RawCommands.Run(commandArgs, app),
        "page" => PageCommands.Run(commandArgs, app, resolvedVersion),
        "shape" => ShapeCommands.Run(commandArgs, app, resolvedVersion),
        _ => UnknownCommand(command),
    };
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

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    return 1;
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
