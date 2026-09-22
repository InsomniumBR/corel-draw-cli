using Corel.Schema;

namespace Corel.Automation.Tests;

public class InstallStoreTests
{
    private static CapabilityManifest BuildManifest(string version, int schemaVersion) => new()
    {
        SchemaVersion = schemaVersion,
        CorelVersion = version,
        ProgId = $"CorelDRAW.Application.{version}",
        TlbPath = @"C:\fake\vgcoreauto.tlb",
    };

    [Fact]
    public void LoadManifest_ReturnsManifest_WhenSchemaVersionMatchesCurrent()
    {
        string version = $"test-{Guid.NewGuid():N}";
        CapabilityManifest saved = BuildManifest(version, CapabilityManifest.CurrentSchemaVersion);
        InstallStore.SaveManifest(saved);
        try
        {
            CapabilityManifest? loaded = InstallStore.LoadManifest(version);
            Assert.NotNull(loaded);
            Assert.Equal(version, loaded!.CorelVersion);
        }
        finally
        {
            File.Delete(InstallStore.ManifestPathFor(version));
        }
    }

    [Fact]
    public void LoadManifest_ReturnsNull_WhenSchemaVersionIsNewerThanCurrent()
    {
        string version = $"test-{Guid.NewGuid():N}";
        CapabilityManifest saved = BuildManifest(version, CapabilityManifest.CurrentSchemaVersion + 1);
        InstallStore.SaveManifest(saved);
        try
        {
            CapabilityManifest? loaded = InstallStore.LoadManifest(version);
            Assert.Null(loaded);
        }
        finally
        {
            File.Delete(InstallStore.ManifestPathFor(version));
        }
    }

    [Fact]
    public void LoadManifest_ReturnsNull_WhenFileDoesNotExist()
    {
        string version = $"test-{Guid.NewGuid():N}";
        Assert.Null(InstallStore.LoadManifest(version));
    }
}
