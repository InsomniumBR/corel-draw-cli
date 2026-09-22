using Corel.Cli.Commands;
using Corel.Schema;

namespace Corel.Cli.Tests;

public class ShapeTypeNamesTests
{
    private static CapabilityManifest BuildManifest() => new()
    {
        CorelVersion = "17",
        ProgId = "CorelDRAW.Application.17",
        TlbPath = @"C:\fake\vgcoreauto.tlb",
        Interfaces = new Dictionary<string, InterfaceInfo>
        {
            ["cdrShapeType"] = new InterfaceInfo
            {
                Kind = "enum",
                Members = new Dictionary<string, MemberInfo>
                {
                    ["cdrRectangleShape"] = new MemberInfo { Kind = "const", Value = 1 },
                    ["cdrEllipseShape"] = new MemberInfo { Kind = "const", Value = 2 },
                },
            },
        },
    };

    [Fact]
    public void Resolve_ReturnsRealName_WhenManifestHasTheValue()
    {
        Assert.Equal("cdrRectangleShape", ShapeTypeNames.Resolve(1, BuildManifest()));
        Assert.Equal("cdrEllipseShape", ShapeTypeNames.Resolve(2, BuildManifest()));
    }

    [Fact]
    public void Resolve_FallsBackToRawValue_WhenManifestIsNull()
    {
        Assert.Equal("1", ShapeTypeNames.Resolve(1, manifest: null));
    }

    [Fact]
    public void Resolve_FallsBackToRawValue_WhenValueIsNotInTheEnum()
    {
        Assert.Equal("99", ShapeTypeNames.Resolve(99, BuildManifest()));
    }

    [Fact]
    public void Resolve_FallsBackToRawValue_WhenManifestHasNoShapeTypeInterface()
    {
        var manifestWithoutShapeType = new CapabilityManifest
        {
            CorelVersion = "17",
            ProgId = "CorelDRAW.Application.17",
            TlbPath = @"C:\fake\vgcoreauto.tlb",
        };
        Assert.Equal("1", ShapeTypeNames.Resolve(1, manifestWithoutShapeType));
    }

    [Fact]
    public void Resolve_ReturnsNullLiteral_ForNullRawType()
    {
        Assert.Equal("null", ShapeTypeNames.Resolve(null, BuildManifest()));
    }
}
