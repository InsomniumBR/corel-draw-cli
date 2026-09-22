using System.Text;

namespace Corel.Cli;

/// <summary>Renders <see cref="CommandGrammar"/> as help. Never touches COM.</summary>
public static class HelpText
{
    public static string Overview()
    {
        var sb = new StringBuilder();
        sb.AppendLine("corel - drive an already-running CorelDRAW instance via COM automation.");
        sb.AppendLine();
        sb.AppendLine("Usage: corel [--corel-version N] <command> [args...]");
        sb.AppendLine("       corel help <command>        detail for one command");
        sb.AppendLine();
        sb.AppendLine("Commands:");
        foreach (CommandSpec spec in CommandGrammar.Commands)
        {
            sb.AppendLine($"  {spec.Name,-20} {spec.Summary}");
        }
        sb.AppendLine();
        sb.AppendLine("The same grammar drives one-shot commands, `corel shell`, and .cdx scripts");
        sb.AppendLine("(`corel script run <file.cdx>`), minus the `corel` prefix inside the last two.");
        sb.Append("--corel-version must come before the command; every other flag belongs to its own command.");
        return sb.ToString();
    }

    public static string? ForCommand(string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return Overview();
        }

        IReadOnlyList<CommandSpec> matches = CommandGrammar.Find(tokens) is CommandSpec exact
            ? [exact]
            : CommandGrammar.InGroup(tokens[0]);

        if (matches.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (CommandSpec spec in matches)
        {
            sb.AppendLine(spec.Usage);
            sb.AppendLine($"  {spec.Summary}");
            foreach (FlagSpec flag in spec.Flags)
            {
                string shape = flag.Kind == FlagKind.Boolean ? "" : $" <{flag.Value.ToString().ToLowerInvariant()}>";
                string need = flag.Required ? "required" : "optional";
                sb.AppendLine($"    --{flag.Name}{shape}  ({need})");
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
