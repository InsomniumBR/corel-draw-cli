using Corel.Schema;
using Corel.Verbs;

namespace Corel.Verbs.Tests;

public class VerbAvailabilityTests
{
    private static VerbDefinition BuildVerb(string? requiresInterface, string? requiresMember) => new()
    {
        Name = "test.verb",
        Group = "test",
        Raw = new VerbRawBinding { Call = "Foo.Bar" },
        RequiresInterface = requiresInterface,
        RequiresMember = requiresMember,
    };

    private static CapabilityManifest BuildManifest() => new()
    {
        CorelVersion = "17",
        ProgId = "CorelDRAW.Application.17",
        TlbPath = @"C:\fake.tlb",
        Interfaces = new Dictionary<string, InterfaceInfo>
        {
            ["IVGLayer"] = new InterfaceInfo
            {
                Kind = "dispatch",
                Members = new Dictionary<string, MemberInfo>
                {
                    ["CreateRectangle"] = new MemberInfo { Kind = "method", Params = ["Left", "Top", "Right", "Bottom"] },
                },
            },
        },
    };

    [Fact]
    public void IsAvailable_True_WhenVerbDeclaresNoRequirement()
    {
        AvailabilityResult result = VerbAvailability.IsAvailable(BuildVerb(null, null), BuildManifest());
        Assert.Equal(AvailabilityStatus.Available, result.Status);
    }

    [Fact]
    public void IsAvailable_True_WhenInterfaceAndMemberBothPresent()
    {
        AvailabilityResult result = VerbAvailability.IsAvailable(
            BuildVerb("IVGLayer", "CreateRectangle"), BuildManifest());
        Assert.Equal(AvailabilityStatus.Available, result.Status);
    }

    [Fact]
    public void IsAvailable_False_WhenInterfaceMissing()
    {
        AvailabilityResult result = VerbAvailability.IsAvailable(
            BuildVerb("IVGPowerClip", "SetContents"), BuildManifest());
        Assert.Equal(AvailabilityStatus.Unavailable, result.Status);
        Assert.Contains("IVGPowerClip", result.Reason);
    }

    [Fact]
    public void IsAvailable_False_WhenInterfacePresentButMemberMissing()
    {
        AvailabilityResult result = VerbAvailability.IsAvailable(
            BuildVerb("IVGLayer", "SomeFutureMethod"), BuildManifest());
        Assert.Equal(AvailabilityStatus.Unavailable, result.Status);
        Assert.Contains("SomeFutureMethod", result.Reason);
    }
}
