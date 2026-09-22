using System.Reflection;
using Corel.Cli.Commands;

namespace Corel.Cli.Tests;

public class MacroCommandsTests
{
    private sealed class FakeComponents
    {
        private readonly bool _exists;

        public FakeComponents(bool exists) => _exists = exists;

        public bool RemoveCalled { get; private set; }

        public object Item(string name)
            => _exists ? new object() : throw new InvalidOperationException("not found");

        public void Remove(object component) => RemoveCalled = true;
    }

    private sealed class LockedProjectComponents
    {
        public object Item(string name) => new object();

        public void Remove(object component) => throw new InvalidOperationException("project is locked");
    }

    [Fact]
    public void RemoveIfExists_DoesNothing_WhenModuleNotFound()
    {
        var components = new FakeComponents(exists: false);

        InvokeRemoveIfExists(components, "Missing");

        Assert.False(components.RemoveCalled);
    }

    [Fact]
    public void RemoveIfExists_RemovesModule_WhenItExists()
    {
        var components = new FakeComponents(exists: true);

        InvokeRemoveIfExists(components, "Existing");

        Assert.True(components.RemoveCalled);
    }

    [Fact]
    public void RemoveIfExists_PropagatesGenuineRemoveFailure_RatherThanSwallowingIt()
    {
        var components = new LockedProjectComponents();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => InvokeRemoveIfExists(components, "Existing"));

        Assert.Equal("project is locked", ex.Message);
    }

    private static void InvokeRemoveIfExists(object components, string name)
    {
        MethodInfo method = typeof(MacroCommands).GetMethod(
            "RemoveIfExists", BindingFlags.NonPublic | BindingFlags.Static)!;
        try
        {
            method.Invoke(null, [components, name]);
        }
        catch (TargetInvocationException ex)
        {
            throw Unwrap(ex);
        }
    }

    private static Exception Unwrap(Exception ex)
    {
        while (ex is TargetInvocationException { InnerException: { } inner })
        {
            ex = inner;
        }
        return ex;
    }
}
