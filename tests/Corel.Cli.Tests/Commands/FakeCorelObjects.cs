namespace Corel.Cli.Tests.Commands;

/// <summary>
/// Plain .NET stand-ins for CorelDRAW's COM object graph. ComInvoker calls
/// <c>Type.InvokeMember</c> against whatever object it's given - that binds against
/// ordinary reflection just as well as a real COM object, so these let
/// PageCommands/ShapeCommands be exercised without a live CorelDRAW instance.
/// </summary>
public sealed class FakeApplication
{
    public FakeDocument ActiveDocument { get; set; } = new();
}

public sealed class FakeDocument
{
    public int Unit { get; set; } = 1;
    public FakePage ActivePage { get; set; } = new();
    public FakeLayer ActiveLayer { get; set; } = new();
}

public sealed class FakePage
{
    public double SizeWidth { get; set; } = 10;
    public double SizeHeight { get; set; } = 20;
    public bool SetSizeCalled { get; private set; }

    public void SetSize(double width, double height)
    {
        SetSizeCalled = true;
        SizeWidth = width;
        SizeHeight = height;
    }
}

public sealed class FakeLayer
{
    public List<FakeShape> Created { get; } = [];

    public FakeShape CreateRectangle(
        double left, double top, double right, double bottom,
        double cornerTL, double cornerTR, double cornerBR, double cornerBL)
    {
        var shape = new FakeShape { Left = left, Top = top, Right = right, Bottom = bottom };
        Created.Add(shape);
        return shape;
    }
}

public sealed class FakeShape
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
}
