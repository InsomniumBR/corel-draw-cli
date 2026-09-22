using Corel.Automation;
using Corel.Schema;

namespace Corel.Cli.Commands;

public static class DetectCommand
{
    public static int Run(string[] args)
    {
        List<CorelInstall> installs = CorelInstallDetector.DetectInstalls();
        if (installs.Count == 0)
        {
            Console.Error.WriteLine("No CorelDRAW installations found on this machine.");
            return 1;
        }

        var record = new InstallRecord { DefaultVersion = ResolveDefaultVersion(installs) };
        record.Installs.AddRange(installs);
        InstallStore.Save(record);

        Console.WriteLine($"Detected {installs.Count} install(s):");
        foreach (CorelInstall install in installs)
        {
            string marker = install.Version == record.DefaultVersion ? " (default)" : "";
            Console.WriteLine($"  {install.Version}: {install.ProgId} at {install.InstallPath}{marker}");
        }
        return 0;
    }

    /// <summary>Re-picks the default from this run's installs so re-running `detect` can actually change it.</summary>
    internal static string ResolveDefaultVersion(IReadOnlyList<CorelInstall> installs) => installs[0].Version;
}
