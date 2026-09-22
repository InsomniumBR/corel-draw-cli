namespace Corel.Cli.Tests;

// VerbGate.WarnIfUnavailable is tightly coupled to InstallStore's hardcoded
// %LOCALAPPDATA%\corel-draw-cli\schemas path with no injection seam, and this project's
// own testing convention (AGENTS.md "Tests" section: pure logic only) argues against
// writing a real manifest file to disk just to exercise a test. These tests cover every
// branch reachable WITHOUT writing one - the "manifest present, interface actually
// missing/present" decision itself is VerbAvailability.IsAvailable, already covered
// directly and thoroughly by Corel.Verbs.Tests.VerbAvailabilityTests (that IS the entire
// decision logic VerbGate delegates to once it has a manifest in hand). Not re-verified
// here: that VerbGate prints the exact right console message once InstallStore actually
// resolves a real manifest file - that wiring was verified live during M4/M5 (see task
// board history) and would need a real file (or a DI seam this task deliberately isn't
// adding, to avoid conflicting with task 15's planned changes to this same file) to cover
// with an automated test.
public class VerbGateTests
{
    [Fact]
    public void WarnIfUnavailable_DoesNothing_ForUnknownVerb()
    {
        string stderr = ConsoleCapture.StdErr(() => VerbGate.WarnIfUnavailable("does.not-exist", "17"));
        Assert.Equal("", stderr);
    }

    [Fact]
    public void WarnIfUnavailable_DoesNothing_WhenCorelVersionIsNull()
    {
        string stderr = ConsoleCapture.StdErr(() => VerbGate.WarnIfUnavailable("shape.create-rect", null));
        Assert.Equal("", stderr);
    }

    [Fact]
    public void WarnIfUnavailable_DoesNothing_WhenNoManifestExistsForVersion()
    {
        // A GUID-based version string is guaranteed to have no cached manifest on disk,
        // exercising InstallStore.LoadManifest's "file doesn't exist" branch - this only
        // ever performs a read-only File.Exists check, it never writes a manifest file.
        string fakeVersion = $"test-{Guid.NewGuid():N}";
        string stderr = ConsoleCapture.StdErr(() => VerbGate.WarnIfUnavailable("shape.create-rect", fakeVersion));
        Assert.Equal("", stderr);
    }

    [Fact]
    public void WarnIfUnavailable_RepeatedCallsForSameMissingVersion_RemainSilent()
    {
        // Exercises VerbGate's per-version ManifestCache being populated (with a null
        // entry) on the first call and reused on the second, without needing to observe
        // the cache directly - both calls should behave identically (silent).
        string fakeVersion = $"test-{Guid.NewGuid():N}";
        string first = ConsoleCapture.StdErr(() => VerbGate.WarnIfUnavailable("shape.create-rect", fakeVersion));
        string second = ConsoleCapture.StdErr(() => VerbGate.WarnIfUnavailable("shape.create-rect", fakeVersion));
        Assert.Equal("", first);
        Assert.Equal("", second);
    }
}
