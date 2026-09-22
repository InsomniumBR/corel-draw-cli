using Corel.Schema;

namespace Corel.Cli.Commands;

/// <summary>Resolves a named CorelDRAW enum constant to its numeric value using the cached
/// capability manifest, so an option word never depends on a magic number typed from memory.</summary>
public static class CorelEnums
{
    public static long Value(CapabilityManifest? manifest, string enumName, string memberName)
    {
        if (manifest is null)
        {
            throw new InvalidOperationException(
                $"Resolving '{memberName}' needs this CorelDRAW version's capability manifest. "
                + "Run `corel schema generate` once, then retry.");
        }

        if (!manifest.Interfaces.TryGetValue(enumName, out InterfaceInfo? info))
        {
            throw new InvalidOperationException(
                $"Enum '{enumName}' is not in this CorelDRAW version's object model, so '{memberName}' cannot be resolved.");
        }

        if (!info.Members.TryGetValue(memberName, out MemberInfo? member) || member.Value is null)
        {
            throw new InvalidOperationException(
                $"'{memberName}' is not a constant of '{enumName}' in this CorelDRAW version.");
        }

        return member.Value.Value;
    }

    /// <summary>Maps an option word to an enum member name, then that name to its value.</summary>
    public static long FromOption(
        CapabilityManifest? manifest,
        string enumName,
        IReadOnlyDictionary<string, string> optionToMember,
        string flagName,
        string option)
    {
        if (!optionToMember.TryGetValue(option, out string? memberName))
        {
            throw new ArgumentException(
                $"Unknown --{flagName} value '{option}'. Expected: {string.Join(", ", optionToMember.Keys)}.");
        }
        return Value(manifest, enumName, memberName);
    }
}
