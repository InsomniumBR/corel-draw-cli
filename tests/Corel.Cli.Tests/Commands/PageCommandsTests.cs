using Corel.Cli.Commands;

namespace Corel.Cli.Tests.Commands;

public class PageCommandsTests
{
    [Fact]
    public void Run_InvalidWidth_ThrowsBeforeMutatingUnit()
    {
        var app = new FakeApplication();
        string[] args = ["size", "--width", "abc", "--height", "1", "--unit", "mm"];

        Assert.ThrowsAny<FormatException>(() => PageCommands.Run(args, app, null));

        Assert.Equal(1, app.ActiveDocument.Unit);
        Assert.False(app.ActiveDocument.ActivePage.SetSizeCalled);
    }

    [Fact]
    public void Run_InvalidUnit_ThrowsBeforeMutatingUnit()
    {
        var app = new FakeApplication();
        string[] args = ["size", "--width", "5", "--height", "5", "--unit", "bogus"];

        Assert.Throws<ArgumentException>(() => PageCommands.Run(args, app, null));

        Assert.Equal(1, app.ActiveDocument.Unit);
        Assert.False(app.ActiveDocument.ActivePage.SetSizeCalled);
    }

    [Fact]
    public void Run_WidthWithoutHeight_IsAHardErrorNotASilentRead()
    {
        var app = new FakeApplication();
        app.ActiveDocument.ActivePage.SizeWidth = 99;
        app.ActiveDocument.ActivePage.SizeHeight = 99;
        string[] args = ["size", "--width", "5"];

        TextWriter originalOut = Console.Out;
        TextWriter originalErr = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        int result;
        try
        {
            result = PageCommands.Run(args, app, null);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }

        Assert.Equal(1, result);
        Assert.False(app.ActiveDocument.ActivePage.SetSizeCalled);
        Assert.DoesNotContain("99", stdout.ToString());
        Assert.Equal(1, app.ActiveDocument.Unit);
    }

    [Fact]
    public void Run_HeightWithoutWidth_IsAHardErrorNotASilentRead()
    {
        var app = new FakeApplication();
        string[] args = ["size", "--height", "5"];

        int result = PageCommands.Run(args, app, null);

        Assert.Equal(1, result);
        Assert.False(app.ActiveDocument.ActivePage.SetSizeCalled);
    }

    [Fact]
    public void Run_ValidWidthAndHeight_SetsUnitThenSize()
    {
        var app = new FakeApplication();
        string[] args = ["size", "--width", "12", "--height", "19", "--unit", "mm"];

        int result = PageCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(3, app.ActiveDocument.Unit); // mm == 3, see UnitNames
        Assert.True(app.ActiveDocument.ActivePage.SetSizeCalled);
        Assert.Equal(12, app.ActiveDocument.ActivePage.SizeWidth);
        Assert.Equal(19, app.ActiveDocument.ActivePage.SizeHeight);
    }

    [Fact]
    public void Run_NoArgs_ReadsCurrentSizeWithoutMutatingUnit()
    {
        var app = new FakeApplication();
        string[] args = ["size"];

        int result = PageCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(1, app.ActiveDocument.Unit);
        Assert.False(app.ActiveDocument.ActivePage.SetSizeCalled);
    }
}
