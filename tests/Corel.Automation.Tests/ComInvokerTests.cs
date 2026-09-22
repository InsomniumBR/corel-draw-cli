namespace Corel.Automation.Tests;

public class ComInvokerTests
{
    private sealed class FakeMiddle
    {
        public object? Leaf { get; set; }
    }

    private sealed class FakeRoot
    {
        public FakeMiddle? Middle { get; set; }

        public object? Close() => null;
    }

    [Fact]
    public void ResolvePath_ReturnsValue_ForNormalChain()
    {
        var root = new FakeRoot { Middle = new FakeMiddle { Leaf = "value" } };

        object? result = ComInvoker.ResolvePath(root, "Middle.Leaf");

        Assert.Equal("value", result);
    }

    [Fact]
    public void ResolvePath_ThrowsWalkingFailureMessage_WhenIntermediateSegmentIsNull()
    {
        var root = new FakeRoot { Middle = null };

        var ex = Assert.Throws<InvalidOperationException>(() => ComInvoker.ResolvePath(root, "Middle.Leaf"));

        Assert.Equal("'Middle' resolved to null while walking path 'Middle.Leaf'.", ex.Message);
    }

    [Fact]
    public void ResolvePath_ThrowsInvokedNoValueMessage_WhenLastSegmentIsNull()
    {
        var root = new FakeRoot { Middle = new FakeMiddle { Leaf = null } };

        var ex = Assert.Throws<InvalidOperationException>(() => ComInvoker.ResolvePath(root, "Middle.Leaf"));

        Assert.Equal(
            "'Leaf' was invoked and returned no value (it may have performed an action, e.g. closing/quitting).",
            ex.Message);
    }

    [Fact]
    public void ResolvePath_ThrowsInvokedNoValueMessage_WhenFinalCallSucceedsButReturnsNull()
    {
        var root = new FakeRoot();

        var ex = Assert.Throws<InvalidOperationException>(() => ComInvoker.ResolvePath(root, "Close"));

        Assert.Equal(
            "'Close' was invoked and returned no value (it may have performed an action, e.g. closing/quitting).",
            ex.Message);
    }
}
