using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Corel.Schema;
using Microsoft.Win32;

namespace Corel.Automation;

/// <summary>
/// Finds CorelDRAW installations on this machine by enumerating versioned ProgIDs
/// (CorelDRAW.Application.&lt;N&gt;) under HKLM\SOFTWARE\Classes.
/// </summary>
/// <remarks>
/// Verified against CorelDRAW X7's actual registry entries: CorelDRAW.Application.17\CLSID
/// gives a CLSID, and CLSID\{...}\LocalServer32's default value is a quoted exe path plus
/// a trailing command-line flag (e.g. `"...\CorelDRW.exe" /Automation`) - the quoted
/// portion must be extracted, not the whole string. Only verified against X7 (.17); other
/// versions' ProgID numbering is assumed to follow the same pattern but is unconfirmed.
/// </remarks>
[SupportedOSPlatform("windows")]
public static partial class CorelInstallDetector
{
    [GeneratedRegex(@"^CorelDRAW\.Application\.(\d+)$")]
    private static partial Regex VersionedProgIdPattern();

    public static List<CorelInstall> DetectInstalls()
    {
        var results = new List<CorelInstall>();

        using RegistryKey? classesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes");
        if (classesKey is null)
        {
            return results;
        }

        foreach (string subKeyName in classesKey.GetSubKeyNames())
        {
            Match match = VersionedProgIdPattern().Match(subKeyName);
            if (!match.Success)
            {
                continue;
            }

            CorelInstall? install = TryResolve(classesKey, subKeyName, match.Groups[1].Value);
            if (install is not null)
            {
                results.Add(install);
            }
        }

        return [.. results.OrderBy(i => int.Parse(i.Version))];
    }

    private static CorelInstall? TryResolve(RegistryKey classesKey, string progId, string version)
    {
        using RegistryKey? clsidRefKey = classesKey.OpenSubKey($@"{progId}\CLSID");
        if (clsidRefKey?.GetValue(null) is not string clsid)
        {
            return null;
        }

        using RegistryKey? localServerKey = classesKey.OpenSubKey($@"CLSID\{clsid}\LocalServer32");
        if (localServerKey?.GetValue(null) is not string rawCommand)
        {
            return null;
        }

        string? exePath = ExtractExecutablePath(rawCommand);
        string? installDir = exePath is null ? null : Path.GetDirectoryName(exePath);
        if (installDir is null)
        {
            return null;
        }

        string tlbPath = Path.Combine(installDir, "vgcoreauto.tlb");
        if (!File.Exists(tlbPath))
        {
            return null;
        }

        return new CorelInstall
        {
            Version = version,
            ProgId = progId,
            InstallPath = installDir,
            TlbPath = tlbPath,
        };
    }

    /// <summary>
    /// A LocalServer32 default value is a quoted exe path optionally followed by
    /// command-line flags (e.g. `"C:\...\CorelDRW.exe" /Automation`), or occasionally an
    /// unquoted bare path with no flags. Internal + tested directly since this string
    /// format is easy to get subtly wrong.
    /// </summary>
    internal static string? ExtractExecutablePath(string rawCommand)
    {
        rawCommand = rawCommand.Trim();
        if (rawCommand.StartsWith('"'))
        {
            int endQuote = rawCommand.IndexOf('"', 1);
            return endQuote > 0 ? rawCommand[1..endQuote] : null;
        }

        int firstSpace = rawCommand.IndexOf(' ');
        return firstSpace > 0 ? rawCommand[..firstSpace] : rawCommand;
    }
}
