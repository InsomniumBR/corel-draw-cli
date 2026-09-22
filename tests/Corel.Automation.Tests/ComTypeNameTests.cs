using System.Runtime.Versioning;

namespace Corel.Automation.Tests;

[SupportedOSPlatform("windows")]
public class ComTypeNameTests
{
    [Fact]
    public void TryResolve_ReturnsNull_ForNull()
    {
        Assert.Null(ComTypeName.TryResolve(null));
    }

    [Fact]
    public void TryResolve_ReturnsNull_ForNonComObject()
    {
        Assert.Null(ComTypeName.TryResolve("a plain string"));
        Assert.Null(ComTypeName.TryResolve(42));
        Assert.Null(ComTypeName.TryResolve(new object()));
    }
}
