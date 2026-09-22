using Corel.Verbs;

namespace Corel.Verbs.Tests;

public class VerbRegistryTests
{
    [Fact]
    public void LoadEmbedded_FindsKnownVerbs()
    {
        VerbRegistry registry = VerbRegistry.LoadEmbedded();

        Assert.NotEmpty(registry.Verbs);
        VerbDefinition? createRect = registry.Find("shape.create-rect");
        Assert.NotNull(createRect);
        Assert.Equal("shape", createRect!.Group);
        Assert.Equal("ActiveDocument.ActiveLayer.CreateRectangle", createRect.Raw.Call);
        Assert.Equal("IVGLayer", createRect.RequiresInterface);
        Assert.Equal("CreateRectangle", createRect.RequiresMember);
    }

    [Fact]
    public void Find_ReturnsNull_ForUnknownVerb()
    {
        VerbRegistry registry = VerbRegistry.LoadEmbedded();
        Assert.Null(registry.Find("does.not-exist"));
    }

    [Fact]
    public void EveryVerb_HasExactlyOneRawOperation()
    {
        VerbRegistry registry = VerbRegistry.LoadEmbedded();

        foreach (VerbDefinition verb in registry.Verbs)
        {
            int setCount = new[] { verb.Raw.Get, verb.Raw.Set, verb.Raw.Call }.Count(v => v is not null);
            Assert.True(setCount == 1, $"Verb '{verb.Name}' must declare exactly one of get/set/call, found {setCount}.");
        }
    }

    [Fact]
    public void Registry_DoesNotContainVerbsWithNoCorrespondingCommand()
    {
        // Regression guard: these had no VerbGate call site anywhere and were removed.
        VerbRegistry registry = VerbRegistry.LoadEmbedded();

        Assert.Null(registry.Find("doc.unit.get"));
        Assert.Null(registry.Find("layer.list"));
        Assert.Null(registry.Find("layer.active"));
    }

    [Fact]
    public void ShapeDelete_RawCallIsNotABareMemberName()
    {
        VerbRegistry registry = VerbRegistry.LoadEmbedded();
        VerbDefinition? shapeDelete = registry.Find("shape.delete");

        Assert.NotNull(shapeDelete);
        Assert.NotNull(shapeDelete!.Raw.Call);
        Assert.Contains('.', shapeDelete.Raw.Call!);
    }
}
