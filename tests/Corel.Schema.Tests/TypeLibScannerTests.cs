using System.Runtime.Versioning;

namespace Corel.Schema.Tests;

[SupportedOSPlatform("windows")]
public class TypeLibScannerTests
{
    // Only present on a machine with CorelDRAW X7 installed - not something CI can rely
    // on. Tests below report as SKIPPED (via SkippableFact - visible in test output,
    // distinct from a pass) rather than silently no-op-passing when it's absent, so CI
    // stays green while still giving real verification on a dev machine with X7 installed.
    private const string X7TlbPath =
        @"P:\Program Files\Corel\CorelDRAW Graphics Suite X7\Programs64\vgcoreauto.tlb";

    [SkippableFact]
    public void Scan_ProducesGroundTruthCdrUnitValues_WhenX7IsInstalled()
    {
        Skip.IfNot(File.Exists(X7TlbPath), $"CorelDRAW X7 type library not found at '{X7TlbPath}' on this machine.");

        CapabilityManifest manifest = TypeLibScanner.Scan(X7TlbPath, "17", "CorelDRAW.Application.17");

        InterfaceInfo cdrUnit = manifest.Interfaces["cdrUnit"];
        Assert.Equal("enum", cdrUnit.Kind);
        Assert.Equal(1, cdrUnit.Members["cdrInch"].Value);
        Assert.Equal(3, cdrUnit.Members["cdrMillimeter"].Value);
        Assert.Equal(14, cdrUnit.Members["cdrPoint"].Value);
        Assert.Equal(5, cdrUnit.Members["cdrPixel"].Value);
    }

    [SkippableFact]
    public void Scan_FindsCreateRectangleWithExpectedParams_WhenX7IsInstalled()
    {
        Skip.IfNot(File.Exists(X7TlbPath), $"CorelDRAW X7 type library not found at '{X7TlbPath}' on this machine.");

        CapabilityManifest manifest = TypeLibScanner.Scan(X7TlbPath, "17", "CorelDRAW.Application.17");

        MemberInfo createRect = manifest.Interfaces["IVGLayer"].Members["CreateRectangle"];
        Assert.Equal("method", createRect.Kind);
        Assert.Equal(
            ["Left", "Top", "Right", "Bottom", "CornerUL", "CornerUR", "CornerLR", "CornerLL"],
            createRect.Params);
    }
}
