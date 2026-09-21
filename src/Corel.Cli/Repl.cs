using Corel.Automation;

namespace Corel.Cli;

/// <summary>
/// Interactive session: same grammar as one-shot CLI commands, minus the `corel` prefix,
/// held against one persistent connection so RCWs (and any REPL-side state) survive
/// across a sequence of commands.
/// </summary>
public static class Repl
{
    public static int Run(object app, string? corelVersion)
    {
        object? version = ComInvoker.Get(app, "Version");
        Console.WriteLine($"corel-draw-cli - connected to CorelDRAW ({version}).");
        PrintDocsBanner(app);
        Console.WriteLine("Type `exit` or `quit` to leave.");

        while (true)
        {
            Console.Write("corel> ");
            string? line = Console.ReadLine();
            if (line is null || line.Trim() is "exit" or "quit")
            {
                break;
            }

            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            try
            {
                CommandDispatcher.Dispatch(Tokenize(trimmed), () => (app, corelVersion));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ErrorMessages.Friendly(ex)}");
            }
        }

        return 0;
    }

    private static void PrintDocsBanner(object app)
    {
        int count = Convert.ToInt32(ComInvoker.Get(ComInvoker.Get(app, "Documents")!, "Count"));
        Console.WriteLine(count == 0
            ? "No documents open."
            : $"{count} document(s) open. Use `docs list` / `docs use <name>`.");
    }

    /// <summary>
    /// Splits on whitespace like a shell would, honoring double-quoted segments
    /// (needed for paths - the naive `Split(' ')` this replaced left literal quote
    /// characters in the token, e.g. `"C:\...\file.cdr"` including the quotes, which
    /// silently made CorelDRAW fail the save against a malformed path). Quotes are
    /// stripped from the resulting token, not preserved.
    /// </summary>
    internal static string[] Tokenize(string line)
    {
        List<string> tokens = [];
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        bool tokenStarted = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                tokenStarted = true;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (tokenStarted)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    tokenStarted = false;
                }
                continue;
            }

            current.Append(c);
            tokenStarted = true;
        }

        if (tokenStarted)
        {
            tokens.Add(current.ToString());
        }

        return [.. tokens];
    }
}
