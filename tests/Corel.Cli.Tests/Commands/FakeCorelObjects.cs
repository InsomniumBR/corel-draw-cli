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
    public FakeDocument()
    {
        ActiveLayer = new FakeLayer();
        ActivePage = new FakePage(ActiveLayer.Created);
    }

    public int Unit { get; set; } = 1;
    public FakePage ActivePage { get; set; }
    public FakeLayer ActiveLayer { get; set; }
}

public sealed class FakePage
{
    public FakePage()
        : this([])
    {
    }

    public FakePage(List<FakeShape> shapes) => Shapes = new FakeShapes(shapes);

    public double SizeWidth { get; set; } = 10;
    public double SizeHeight { get; set; } = 20;
    public bool SetSizeCalled { get; private set; }
    public FakeShapes Shapes { get; }

    public void SetSize(double width, double height)
    {
        SetSizeCalled = true;
        SizeWidth = width;
        SizeHeight = height;
    }
}

public sealed class FakeShapes(List<FakeShape> shapes)
{
    public int Count => shapes.Count;

    /// <summary>The range most recently handed out, so a test can inspect what was called on it.</summary>
    public FakeShapeRange? LastRange { get; private set; }

    public FakeShape Item(int index) => shapes[index - 1];

    public FakeShapeRange All() => LastRange = new FakeShapeRange([.. shapes]);

    public FakeShapeRange Range(int[] indexes)
        => LastRange = new FakeShapeRange([.. indexes.Select(i => shapes[i - 1])]);
}

public sealed class FakeShapeRange(List<FakeShape> shapes)
{
    public List<string> Operations { get; } = [];

    public int Count => shapes.Count;

    public FakeShape Item(int index) => shapes[index - 1];

    public FakeShape FirstShape => shapes[0];

    public void Group() => Operations.Add("Group");

    public void Combine() => Operations.Add("Combine");

    public void AlignToShape(int type, object shape) => Operations.Add($"AlignToShape({type},{shape.GetType().Name})");

    public void AlignRangeToPage(int type) => Operations.Add($"AlignRangeToPage({type})");

    public void AlignRangeToPageCenter(int type) => Operations.Add($"AlignRangeToPageCenter({type})");

    public void AlignRangeToGrid(int type) => Operations.Add($"AlignRangeToGrid({type})");

    public void Distribute(int type, bool overPage) => Operations.Add($"Distribute({type},{overPage})");
}

public sealed class FakeLayer
{
    public List<FakeShape> Created { get; } = [];

    public List<string> Calls { get; } = [];

    public FakeShape CreateRectangle(
        double left, double top, double right, double bottom,
        double cornerTL, double cornerTR, double cornerBR, double cornerBL)
        => Add(left, top, right, bottom);

    public FakeShape CreateEllipse(
        double left, double top, double right, double bottom,
        object? startAngle = null, object? endAngle = null, bool pie = false)
    {
        Calls.Add($"CreateEllipse(pie={pie},start={startAngle},end={endAngle})");
        return Add(left, top, right, bottom);
    }

    public FakeShape CreatePolygon(
        double left, double top, double right, double bottom,
        int sides = 3, object? subPaths = null, object? complexity = null, bool star = false)
    {
        Calls.Add($"CreatePolygon(sides={sides},star={star})");
        return Add(left, top, right, bottom);
    }

    public FakeShape CreateLineSegment(double x1, double y1, double x2, double y2)
    {
        Calls.Add($"CreateLineSegment({x1},{y1},{x2},{y2})");
        return Add(x1, y1, x2, y2);
    }

    public FakeShape CreateArtisticText(
        double left, double bottom, string text,
        object? languageId = null, object? charSet = null, object? font = null, object? size = null, object? bold = null)
    {
        Calls.Add($"CreateArtisticText('{text}',font={font},size={size},bold={bold})");
        return Add(left, bottom, left, bottom);
    }

    public FakeShape CreateParagraphText(
        double left, double top, double right, double bottom, string text,
        object? languageId = null, object? charSet = null, object? font = null, object? size = null, object? bold = null)
    {
        Calls.Add($"CreateParagraphText('{text}',font={font},size={size},bold={bold})");
        return Add(left, top, right, bottom);
    }

    private FakeShape Add(double left, double top, double right, double bottom)
    {
        var shape = new FakeShape { Left = left, Top = top, Right = right, Bottom = bottom };
        Created.Add(shape);
        return shape;
    }
}

public sealed class FakeShape
{
    public string Name { get; set; } = "";
    public int Type { get; set; } = 5;
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }

    public double LeftX => Left;
    public double TopY => Top;
    public double RightX => Right;
    public double BottomY => Bottom;
    public double SizeWidth => Right - Left;
    public double SizeHeight => Top - Bottom;

    public List<string> Operations { get; } = [];
    public FakeFill Fill { get; } = new();
    public FakeOutline Outline { get; } = new();

    public void Delete() => Operations.Add("Delete");

    public void Move(double dx, double dy) => Operations.Add($"Move({dx},{dy})");

    public void SetPosition(double x, double y) => Operations.Add($"SetPosition({x},{y})");

    public void Rotate(double angle) => Operations.Add($"Rotate({angle})");

    public void RotateEx(double angle, double cx, double cy) => Operations.Add($"RotateEx({angle},{cx},{cy})");

    public void SetSize(double width, double height) => Operations.Add($"SetSize({width},{height})");

    public void Stretch(double x, double y, bool characters) => Operations.Add($"Stretch({x},{y})");

    public void Flip(int axes) => Operations.Add($"Flip({axes})");

    public void Duplicate(double offsetX, double offsetY) => Operations.Add($"Duplicate({offsetX},{offsetY})");

    public void OrderToFront() => Operations.Add("OrderToFront");

    public void OrderToBack() => Operations.Add("OrderToBack");

    public void OrderForwardOne() => Operations.Add("OrderForwardOne");

    public void OrderBackOne() => Operations.Add("OrderBackOne");

    public void Ungroup() => Operations.Add("Ungroup");

    public void UngroupAll() => Operations.Add("UngroupAll");

    public void BreakApart() => Operations.Add("BreakApart");
}

public sealed class FakeFill
{
    public FakeColor UniformColor { get; } = new();

    public bool NoFillApplied { get; private set; }

    public void ApplyNoFill() => NoFillApplied = true;
}

public sealed class FakeOutline
{
    public double Width { get; set; }

    public FakeColor Color { get; } = new();

    public bool NoOutlineApplied { get; private set; }

    public void SetNoOutline() => NoOutlineApplied = true;
}

public sealed class FakeColor
{
    public int? Red { get; private set; }
    public int? Green { get; private set; }
    public int? Blue { get; private set; }

    public void RGBAssign(int red, int green, int blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }
}
