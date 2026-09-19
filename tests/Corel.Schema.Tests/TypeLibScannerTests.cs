using System.Runtime.Versioning;
using Corel.Schema;

namespace Corel.Schema.Tests;

[SupportedOSPlatform("windows")]
public class TypeLibScannerTests
{
    // Only present on a machine with CorelDRAW X7 installed - not something CI can rely
    // on. Tests below no-op (rather than fail) when it's absent, so this suite still runs
    // clean in CI while giving real verification on a dev machine that has X7 installed.
    private const string X7TlbPath =
        @"P:\Program Files\Corel\CorelDRAW Graphics Suite X7\Programs64\vgcoreauto.tlb";

    [Fact]
    public void Scan_ProducesGroundTruthCdrUnitValues_WhenX7IsInstalled()
    {
        if (!File.Exists(X7TlbPath))
        {
            return;
        }

        CapabilityManifest manifest = TypeLibScanner.Scan(X7TlbPath, "17", "CorelDRAW.Application.17");

        InterfaceInfo cdrUnit = manifest.Interfaces["cdrUnit"];
        Assert.Equal("enum", cdrUnit.Kind);
        Assert.Equal(1, cdrUnit.Members["cdrInch"].Value);
        Assert.Equal(3, cdrUnit.Members["cdrMillimeter"].Value);
        Assert.Equal(14, cdrUnit.Members["cdrPoint"].Value);
        Assert.Equal(5, cdrUnit.Members["cdrPixel"].Value);
    }

    [Fact]
    public void Scan_FindsCreateRectangleWithExpectedParams_WhenX7IsInstalled()
    {
        if (!File.Exists(X7TlbPath))
        {
            return;
        }

        CapabilityManifest manifest = TypeLibScanner.Scan(X7TlbPath, "17", "CorelDRAW.Application.17");

        MemberInfo createRect = manifest.Interfaces["IVGLayer"].Members["CreateRectangle"];
        Assert.Equal("method", createRect.Kind);
        Assert.Equal(
            ["Left", "Top", "Right", "Bottom", "CornerUL", "CornerUR", "CornerLR", "CornerLL"],
            createRect.Params);
    }
}
