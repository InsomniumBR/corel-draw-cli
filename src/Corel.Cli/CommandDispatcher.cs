using Corel.Cli.Commands;

namespace Corel.Cli;

/// <summary>
/// Shared dispatch used by both one-shot CLI invocations and the interactive REPL (and,
/// in M7, the script runner) - one grammar, three ways to drive it. `connect` is invoked
/// lazily so `detect`/`schema` never force a live COM attach.
/// </summary>
public static class CommandDispatcher
{
    public static int Dispatch(string[] args, Func<(object App, string? Version)> connect)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: corel <command> [args...]");
            return 1;
        }

        string command = args[0];
        string[] commandArgs = args[1..];

        if (command == "detect")
        {
            return DetectCommand.Run(commandArgs);
        }
        if (command == "schema")
        {
            return SchemaCommands.Run(commandArgs);
        }

        (object app, string? version) = connect();
        return command switch
        {
            "raw" => RawCommands.Run(commandArgs, app),
            "page" => PageCommands.Run(commandArgs, app, version),
            "shape" => ShapeCommands.Run(commandArgs, app, version),
            "docs" => DocsCommands.Run(commandArgs, app, version),
            "doc" => DocCommands.Run(commandArgs, app, version),
            "macro" => MacroCommands.Run(commandArgs, app, version),
            _ => UnknownCommand(command),
        };
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        return 1;
    }
}
