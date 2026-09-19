using Corel.Automation;
using Corel.Schema;

namespace Corel.Cli.Commands;

public static class SchemaCommands
{
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] != "generate")
        {
            Console.Error.WriteLine("Usage: corel schema generate [--version N]");
            return 1;
        }

        var reader = new ArgReader(args[1..]);
        InstallRecord record = InstallStore.Load()
            ?? throw new InvalidOperationException("No installs recorded. Run `corel detect` first.");
        CorelInstall install = CorelVersionResolver.Resolve(record, reader.Flag("version"));

        CapabilityManifest manifest = TypeLibScanner.Scan(install.TlbPath, install.Version, install.ProgId);
        InstallStore.SaveManifest(manifest);

        Console.WriteLine($"Generated manifest for CorelDRAW {install.Version}: {manifest.Interfaces.Count} types scanned.");
        Console.WriteLine($"Saved to: {InstallStore.ManifestPathFor(install.Version)}");
        return 0;
    }
}
