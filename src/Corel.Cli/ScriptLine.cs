namespace Corel.Cli;

/// <summary>One executable line of a .cdx script, after variable substitution.</summary>
public sealed record ScriptLine(int Number, string Text, string[] Tokens);
