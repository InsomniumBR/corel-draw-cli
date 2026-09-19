using Corel.Automation;
using Corel.Schema;

namespace Corel.Automation.Tests;

public class CorelVersionResolverTests
{
    private static InstallRecord BuildRecord() => new()
    {
        DefaultVersion = "17",
        Installs =
        [
            new CorelInstall { Version = "17", ProgId = "CorelDRAW.Application.17", InstallPath = @"C:\x7", TlbPath = @"C:\x7\vgcoreauto.tlb" },
            new CorelInstall { Version = "24", ProgId = "CorelDRAW.Application.24", InstallPath = @"C:\2024", TlbPath = @"C:\2024\vgcoreauto.tlb" },
        ],
    };

    [Fact]
    public void Resolve_UsesDefaultVersion_WhenNoneRequested()
    {
        CorelInstall resolved = CorelVersionResolver.Resolve(BuildRecord(), requestedVersion: null);
        Assert.Equal("CorelDRAW.Application.17", resolved.ProgId);
    }

    [Fact]
    public void Resolve_UsesRequestedVersion_WhenGiven()
    {
        CorelInstall resolved = CorelVersionResolver.Resolve(BuildRecord(), requestedVersion: "24");
        Assert.Equal("CorelDRAW.Application.24", resolved.ProgId);
    }

    [Fact]
    public void Resolve_ThrowsWithKnownVersionsListed_ForUnknownVersion()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => CorelVersionResolver.Resolve(BuildRecord(), requestedVersion: "999"));
        Assert.Contains("17", ex.Message);
        Assert.Contains("24", ex.Message);
    }

    [Fact]
    public void Resolve_ThrowsWhenNoInstallsRecorded()
    {
        var empty = new InstallRecord();
        Assert.Throws<InvalidOperationException>(() => CorelVersionResolver.Resolve(empty, requestedVersion: null));
    }

    [Fact]
    public void Resolve_ThrowsWhenNoDefaultAndNoneRequested()
    {
        InstallRecord record = BuildRecord();
        record.DefaultVersion = null;
        Assert.Throws<InvalidOperationException>(() => CorelVersionResolver.Resolve(record, requestedVersion: null));
    }
}
