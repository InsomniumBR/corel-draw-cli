using Corel.Cli.Commands;
using Corel.Schema;

namespace Corel.Cli.Tests;

public class DetectCommandTests
{
    [Fact]
    public void ResolveDefaultVersion_ReturnsTheFirstDetectedInstall()
    {
        List<CorelInstall> installs =
        [
            new() { Version = "17", ProgId = "CorelDRAW.Application.17", InstallPath = "C:\\x", TlbPath = "C:\\x.tlb" },
            new() { Version = "24", ProgId = "CorelDRAW.Application.24", InstallPath = "C:\\y", TlbPath = "C:\\y.tlb" },
        ];

        Assert.Equal("17", DetectCommand.ResolveDefaultVersion(installs));
    }

    [Fact]
    public void ResolveDefaultVersion_ChangesWhenTheFreshlyScannedListChanges()
    {
        // Regression test: re-running `detect` must be able to change the default,
        // rather than freezing whatever was picked on the very first run (README's
        // "Version pinning" section promises re-running `detect` can change it).
        List<CorelInstall> firstRun = [new() { Version = "17", ProgId = "p", InstallPath = "a", TlbPath = "a.tlb" }];
        List<CorelInstall> secondRun = [new() { Version = "24", ProgId = "p", InstallPath = "a", TlbPath = "a.tlb" }];

        Assert.Equal("17", DetectCommand.ResolveDefaultVersion(firstRun));
        Assert.Equal("24", DetectCommand.ResolveDefaultVersion(secondRun));
    }
}
