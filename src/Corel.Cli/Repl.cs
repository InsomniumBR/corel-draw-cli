using System.Text;
using Corel.Automation;

namespace Corel.Cli;

/// <summary>
/// Interactive session: same grammar as one-shot CLI commands, minus the `corel` prefix,
/// held against one persistent connection so RCWs (and any REPL-side state) survive
/// across a sequence of commands.
/// </summary>
public static class Repl
{
    public static int Run(CorelSession session)
    {
        object? version = ComInvoker.Get(session.App, "Version");
        Console.WriteLine($"corel-draw-cli - connected to CorelDRAW ({version}).");
        PrintDocsBanner(session.App);
        Console.WriteLine("Type `help` for commands, `exit` or `quit` to leave.");

        return RunLoop(Console.In, tokens => CommandDispatcher.Dispatch(tokens, session));
    }

    /// <summary>The read-dispatch loop, decoupled from COM. Returns the exit code of the last
    /// command that ran, so a piped session propagates a real status.</summary>
    internal static int RunLoop(TextReader input, Func<string[], int> dispatch)
    {
        int lastExitCode = 0;
        while (true)
        {
            Console.Write("corel> ");
            string? line = input.ReadLine();
            // A piped session's first line can carry a UTF-8 BOM, which would otherwise
            // become part of the command name.
            string trimmed = line?.TrimStart('﻿').Trim() ?? "";
            if (line is null || trimmed is "exit" or "quit")
            {
                break;
            }

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (!TryTokenize(trimmed, out string[] tokens, out string? tokenError))
            {
                Console.Error.WriteLine($"Error: {tokenError}");
                lastExitCode = 1;
                continue;
            }

            try
            {
                lastExitCode = dispatch(tokens);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ErrorMessages.Friendly(ex)}");
                lastExitCode = 1;
            }
        }

        return lastExitCode;
    }

    private static void PrintDocsBanner(object app)
    {
        int count = Convert.ToInt32(ComInvoker.Get(ComInvoker.Get(app, "Documents")!, "Count"));
        Console.WriteLine(count == 0
            ? "No documents open."
            : $"{count} document(s) open. Use `docs list` / `docs use <name>`.");
    }

    /// <summary>Splits on whitespace like a shell would, honoring double-quoted segments and
    /// stripping their quotes. Inside a quoted segment a doubled quote is a literal quote;
    /// backslash is never an escape, because Windows paths are full of them.</summary>
    internal static string[] Tokenize(string line)
        => TryTokenize(line, out string[] tokens, out string? error)
            ? tokens
            : throw new FormatException(error);

    internal static bool TryTokenize(string line, out string[] tokens, out string? error)
    {
        List<string> result = [];
        var current = new StringBuilder();
        bool inQuotes = false;
        bool tokenStarted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                tokenStarted = true;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (tokenStarted)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    tokenStarted = false;
                }
                continue;
            }

            current.Append(c);
            tokenStarted = true;
        }

        if (inQuotes)
        {
            tokens = [];
            error = $"unterminated quote in: {line}";
            return false;
        }

        if (tokenStarted)
        {
            result.Add(current.ToString());
        }

        tokens = [.. result];
        error = null;
        return true;
    }
}
