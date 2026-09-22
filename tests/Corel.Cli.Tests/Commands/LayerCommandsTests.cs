using Corel.Cli.Commands;

namespace Corel.Cli.Tests.Commands;

public class LayerCommandsTests
{
    [Fact]
    public void Add_CreatesLayerOnActivePage()
    {
        var app = new FakeApplication();

        int result = LayerCommands.Run(["add", "--name", "NewLayer"], app, null);

        Assert.Equal(0, result);
        Assert.Equal(2, app.ActiveDocument.ActivePage.Layers.Count);
        Assert.Equal("NewLayer", app.ActiveDocument.ActivePage.Layers.Item(2).Name);
    }

    [Fact]
    public void Add_MissingName_ReturnsUsageError()
    {
        var app = new FakeApplication();

        int result = LayerCommands.Run(["add"], app, null);

        Assert.Equal(1, result);
        Assert.Equal(1, app.ActiveDocument.ActivePage.Layers.Count);
    }

    [Fact]
    public void Use_ByIndex_ActivatesThatLayer()
    {
        var app = new FakeApplication();
        app.ActiveDocument.ActivePage.Layers.CreateLayer("Second");

        int result = LayerCommands.Run(["use", "--index", "2"], app, null);

        Assert.Equal(0, result);
        Assert.Same(app.ActiveDocument.ActivePage.Layers.Item(2), app.ActiveDocument.ActiveLayer);
        Assert.True(app.ActiveDocument.ActivePage.Layers.Item(2).ActivateCalled);
    }

    [Fact]
    public void Use_ByName_ActivatesThatLayer()
    {
        var app = new FakeApplication();
        app.ActiveDocument.ActivePage.Layers.CreateLayer("Second");

        int result = LayerCommands.Run(["use", "--name", "Second"], app, null);

        Assert.Equal(0, result);
        Assert.Equal("Second", app.ActiveDocument.ActiveLayer.Name);
    }

    [Fact]
    public void Use_UnknownName_ReturnsErrorWithoutActivatingAnything()
    {
        var app = new FakeApplication();

        int result = LayerCommands.Run(["use", "--name", "DoesNotExist"], app, null);

        Assert.Equal(1, result);
        Assert.Equal("Layer 1", app.ActiveDocument.ActiveLayer.Name);
    }

    [Fact]
    public void Use_IndexOutOfRange_ReturnsError()
    {
        var app = new FakeApplication();

        int result = LayerCommands.Run(["use", "--index", "5"], app, null);

        Assert.Equal(1, result);
    }

    [Fact]
    public void Use_NeitherNameNorIndex_ReturnsUsageError()
    {
        var app = new FakeApplication();

        int result = LayerCommands.Run(["use"], app, null);

        Assert.Equal(1, result);
    }

    [Fact]
    public void Delete_ByIndex_RemovesLayer()
    {
        var app = new FakeApplication();
        app.ActiveDocument.ActivePage.Layers.CreateLayer("Second");

        int result = LayerCommands.Run(["delete", "--index", "2"], app, null);

        Assert.Equal(0, result);
        Assert.Equal(1, app.ActiveDocument.ActivePage.Layers.Count);
    }

    [Fact]
    public void Delete_ByName_RemovesLayer()
    {
        var app = new FakeApplication();
        app.ActiveDocument.ActivePage.Layers.CreateLayer("Second");

        int result = LayerCommands.Run(["delete", "--name", "Second"], app, null);

        Assert.Equal(0, result);
        Assert.Equal(1, app.ActiveDocument.ActivePage.Layers.Count);
        Assert.Null(app.ActiveDocument.ActivePage.Layers.Find("Second"));
    }

    [Fact]
    public void List_MarksTheActiveLayer()
    {
        var app = new FakeApplication();
        app.ActiveDocument.ActivePage.Layers.CreateLayer("Second");

        TextWriter originalOut = Console.Out;
        var stdout = new StringWriter();
        Console.SetOut(stdout);
        int result;
        try
        {
            result = LayerCommands.Run(["list"], app, null);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Equal(0, result);
        string output = stdout.ToString();
        Assert.Contains("[1]", output);
        Assert.Contains("Layer 1", output);
        Assert.Contains("(active)", output);
        Assert.Contains("[2]", output);
        Assert.Contains("Second", output);
    }
}
