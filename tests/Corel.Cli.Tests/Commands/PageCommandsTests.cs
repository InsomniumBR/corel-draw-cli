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

    [Fact]
    public void Add_NoAfter_AppendsAtTheEnd()
    {
        var app = new FakeApplication();

        int result = PageCommands.Run(["add"], app, null);

        Assert.Equal(0, result);
        Assert.Equal(2, app.ActiveDocument.Pages.Count);
        Assert.Equal("Page 2", app.ActiveDocument.Pages.Item(2).Name);
    }

    [Fact]
    public void Add_WithAfter_InsertsAfterThatPage()
    {
        var app = new FakeApplication();
        app.ActiveDocument.Pages.Item(1).Name = "ORIG";
        app.ActiveDocument.Pages.AddPages(1);
        app.ActiveDocument.Pages.Item(2).Name = "APPENDED"; // [ORIG, APPENDED]

        int result = PageCommands.Run(["add", "--after", "1"], app, null);

        Assert.Equal(0, result);
        Assert.Equal(3, app.ActiveDocument.Pages.Count);
        // Per the live-verified InsertPages(count, beforePage: false, page: 1) semantics,
        // the new page lands strictly between ORIG and APPENDED.
        Assert.Equal("ORIG", app.ActiveDocument.Pages.Item(1).Name);
        Assert.Equal("APPENDED", app.ActiveDocument.Pages.Item(3).Name);
        Assert.NotEqual("ORIG", app.ActiveDocument.Pages.Item(2).Name);
        Assert.NotEqual("APPENDED", app.ActiveDocument.Pages.Item(2).Name);
    }

    [Fact]
    public void Add_NonNumericAfter_ReturnsErrorWithoutMutating()
    {
        var app = new FakeApplication();

        int result = PageCommands.Run(["add", "--after", "abc"], app, null);

        Assert.Equal(1, result);
        Assert.Equal(1, app.ActiveDocument.Pages.Count);
    }

    [Fact]
    public void Delete_ValidIndex_RemovesPageAndRenumbers()
    {
        var app = new FakeApplication();
        app.ActiveDocument.Pages.AddPages(2); // [Page1, Page2, Page3]

        int result = PageCommands.Run(["delete", "--index", "1"], app, null);

        Assert.Equal(0, result);
        Assert.Equal(2, app.ActiveDocument.Pages.Count);
        Assert.True(app.ActiveDocument.Pages.Item(1).Index == 1);
    }

    [Fact]
    public void Delete_MissingIndex_ReturnsUsageError()
    {
        var app = new FakeApplication();

        int result = PageCommands.Run(["delete"], app, null);

        Assert.Equal(1, result);
        Assert.Equal(1, app.ActiveDocument.Pages.Count);
    }

    [Fact]
    public void Goto_ValidIndex_ActivatesThatPage()
    {
        var app = new FakeApplication();
        app.ActiveDocument.Pages.AddPages(1); // [Page1, Page2]

        int result = PageCommands.Run(["goto", "--index", "2"], app, null);

        Assert.Equal(0, result);
        Assert.Same(app.ActiveDocument.Pages.Item(2), app.ActiveDocument.ActivePage);
        Assert.True(app.ActiveDocument.Pages.Item(2).ActivateCalled);
    }

    [Fact]
    public void Goto_NonNumericIndex_ReturnsUsageError()
    {
        var app = new FakeApplication();

        int result = PageCommands.Run(["goto", "--index", "abc"], app, null);

        Assert.Equal(1, result);
    }

    [Fact]
    public void List_MarksTheActivePage()
    {
        var app = new FakeApplication();
        app.ActiveDocument.Pages.AddPages(1); // [Page1(active), Page2]

        TextWriter originalOut = Console.Out;
        var stdout = new StringWriter();
        Console.SetOut(stdout);
        int result;
        try
        {
            result = PageCommands.Run(["list"], app, null);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Equal(0, result);
        string output = stdout.ToString();
        Assert.Contains("[1]", output);
        Assert.Contains("(active)", output);
        Assert.Contains("[2]", output);
    }
}
