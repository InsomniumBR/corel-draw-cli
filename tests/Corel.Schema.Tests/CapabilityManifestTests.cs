namespace Corel.Schema.Tests;

public class CapabilityManifestTests
{
    private static CapabilityManifest BuildSample() => new()
    {
        CorelVersion = "17",
        ProgId = "CorelDRAW.Application.17",
        TlbPath = @"C:\fake\vgcoreauto.tlb",
        Interfaces = new Dictionary<string, InterfaceInfo>
        {
            ["cdrUnit"] = new InterfaceInfo
            {
                Kind = "enum",
                Members = new Dictionary<string, MemberInfo>
                {
                    ["cdrInch"] = new MemberInfo { Kind = "const", Value = 1 },
                    ["cdrMillimeter"] = new MemberInfo { Kind = "const", Value = 3 },
                },
            },
            ["IVGLayer"] = new InterfaceInfo
            {
                Kind = "dispatch",
                Members = new Dictionary<string, MemberInfo>
                {
                    ["CreateRectangle"] = new MemberInfo
                    {
                        Kind = "method",
                        Params = ["Left", "Top", "Right", "Bottom", "CornerUL", "CornerUR", "CornerLR", "CornerLL"],
                    },
                },
            },
        },
    };

    [Fact]
    public void RoundTrip_PreservesEnumValuesAndMethodParams()
    {
        CapabilityManifest original = BuildSample();

        string json = CapabilityManifestSerializer.Serialize(original);
        CapabilityManifest restored = CapabilityManifestSerializer.Deserialize(json);

        Assert.Equal(original.CorelVersion, restored.CorelVersion);
        Assert.Equal(original.ProgId, restored.ProgId);
        Assert.Equal(1, restored.Interfaces["cdrUnit"].Members["cdrInch"].Value);
        Assert.Equal(3, restored.Interfaces["cdrUnit"].Members["cdrMillimeter"].Value);
        Assert.Equal(
            ["Left", "Top", "Right", "Bottom", "CornerUL", "CornerUR", "CornerLR", "CornerLL"],
            restored.Interfaces["IVGLayer"].Members["CreateRectangle"].Params);
    }

    [Fact]
    public void Deserialize_ThrowsOnEmptyJson()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => CapabilityManifestSerializer.Deserialize(""));
    }
}
