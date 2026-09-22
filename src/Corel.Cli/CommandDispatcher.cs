using Corel.Cli.Commands;

namespace Corel.Cli;

/// <summary>Shared dispatch for one-shot invocations, the REPL and the script runner - one
/// grammar, nothing reachable from only one of them. Help and structural validation both run
/// before <see cref="CorelSession.App"/> is touched.</summary>
public static class CommandDispatcher
{
    public static int Dispatch(string[] args, CorelSession session)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine(HelpText.Overview());
            return 1;
        }

        if (TryHelpTarget(args, out string[] helpTarget))
        {
            string? text = HelpText.ForCommand(helpTarget);
            if (text is null)
            {
                Console.Error.WriteLine($"Unknown command '{helpTarget[0]}'. Run `corel help` to see every command.");
                return 1;
            }
            Console.WriteLine(text);
            return 0;
        }

        if (CommandValidator.Validate(args) is string error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        string command = args[0];
        string[] commandArgs = args[1..];

        switch (command)
        {
            case "detect":
                return DetectCommand.Run(commandArgs);
            case "schema":
                return SchemaCommands.Run(commandArgs);
            case "script":
                return RunScript(commandArgs[1..], session);
            case "shell":
                Console.Error.WriteLine("`shell` starts an interactive session and can only be run from a terminal, not from inside one or from a script.");
                return 1;
        }

        return command switch
        {
            "raw" => RawCommands.Run(commandArgs, session.App),
            "page" => PageCommands.Run(commandArgs, session.App, session.Version),
            "shape" => ShapeCommands.Run(commandArgs, session.App, session.Version),
            "docs" => DocsCommands.Run(commandArgs, session.App, session.Version),
            "layer" => LayerCommands.Run(commandArgs, session.App, session.Version),
            "doc" => DocCommands.Run(commandArgs, session.App, session.Version),
            "macro" => MacroCommands.Run(commandArgs, session.App, session.Version),
            "image" => ImageCommands.Run(commandArgs, session.App, session.Version),
            _ => Unreachable(command),
        };
    }

    private static int RunScript(string[] args, CorelSession session)
    {
        ParsedArgs parsed = CommandValidator.Parse(CommandGrammar.Find(["script", "run"])!, args);
        return ScriptRunner.Run(
            parsed.Positional[0],
            session,
            ScriptVars.Parse(parsed.Value("vars")),
            parsed.Has("force"));
    }

    private static bool TryHelpTarget(string[] args, out string[] target)
    {
        if (args[0] is "help" or "--help" or "-h")
        {
            target = args[1..];
            return true;
        }
        if (args.Any(a => a is "--help" or "-h"))
        {
            target = [.. args.Where(a => a is not ("--help" or "-h"))];
            return true;
        }
        target = [];
        return false;
    }

    private static int Unreachable(string command)
        => throw new InvalidOperationException($"Command '{command}' is in the grammar but has no handler.");
}
