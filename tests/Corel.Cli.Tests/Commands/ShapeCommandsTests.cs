using Corel.Cli.Commands;

namespace Corel.Cli.Tests.Commands;

public class ShapeCommandsTests
{
    [Fact]
    public void CreateRect_InvalidUnit_ThrowsBeforeMutatingUnit()
    {
        var app = new FakeApplication();
        string[] args = ["create-rect", "--left", "0", "--top", "1", "--right", "1", "--bottom", "0", "--unit", "bogus"];

        Assert.Throws<ArgumentException>(() => ShapeCommands.Run(args, app, null));

        Assert.Equal(1, app.ActiveDocument.Unit);
        Assert.Empty(app.ActiveDocument.ActiveLayer.Created);
    }

    [Fact]
    public void CreateRect_InvalidLeft_ThrowsBeforeMutatingUnit()
    {
        var app = new FakeApplication();
        string[] args = ["create-rect", "--left", "abc", "--top", "1", "--right", "1", "--bottom", "0", "--unit", "mm"];

        Assert.ThrowsAny<FormatException>(() => ShapeCommands.Run(args, app, null));

        Assert.Equal(1, app.ActiveDocument.Unit);
        Assert.Empty(app.ActiveDocument.ActiveLayer.Created);
    }

    [Fact]
    public void CreateRect_MissingRequiredFlag_ThrowsBeforeMutatingUnit()
    {
        var app = new FakeApplication();
        string[] args = ["create-rect", "--top", "1", "--right", "1", "--bottom", "0", "--unit", "mm"];

        Assert.Throws<ArgumentException>(() => ShapeCommands.Run(args, app, null));

        Assert.Equal(1, app.ActiveDocument.Unit);
        Assert.Empty(app.ActiveDocument.ActiveLayer.Created);
    }

    [Fact]
    public void CreateRect_ValidArgs_SetsUnitThenCreatesRectangle()
    {
        var app = new FakeApplication();
        string[] args = ["create-rect", "--left", "0", "--top", "1", "--right", "1", "--bottom", "0", "--unit", "in"];

        int result = ShapeCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(1, app.ActiveDocument.Unit); // "in" == 1, see UnitNames
        FakeShape shape = Assert.Single(app.ActiveDocument.ActiveLayer.Created);
        Assert.Equal(0, shape.Left);
        Assert.Equal(1, shape.Top);
        Assert.Equal(1, shape.Right);
        Assert.Equal(0, shape.Bottom);
    }

    [Fact]
    public void CreateRect_NoUnitFlag_DoesNotTouchUnit()
    {
        var app = new FakeApplication();
        string[] args = ["create-rect", "--left", "0", "--top", "1", "--right", "1", "--bottom", "0"];

        int result = ShapeCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(1, app.ActiveDocument.Unit);
        Assert.Single(app.ActiveDocument.ActiveLayer.Created);
    }
}
