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

    public FakeExportOptions CreateStructExportOptions() => new();

    public FakePaletteOptions CreateStructPaletteOptions() => new();
}

public sealed class FakeDocument
{
    public int Unit { get; set; } = 1;
    public FakePages Pages { get; }
    public FakePage ActivePage { get; set; }
    public FakeLayer ActiveLayer { get; set; }
    public string? ExportedPath { get; private set; }
    public int? ExportedFilter { get; private set; }
    public int? ExportedRange { get; private set; }
    public FakeExportOptions? ExportedOptions { get; private set; }
    public string? PublishedPdfPath { get; private set; }
    public int UndoLevels { get; private set; }
    public int RedoLevels { get; private set; }

    public FakeDocument()
    {
        Pages = new FakePages();
        Pages.OnPageCreated = p =>
        {
            p.OnActivate = () => ActivePage = p;
            p.Layers.OnLayerCreated = l => l.OnActivate = () => ActiveLayer = l;
        };
        ActivePage = Pages.AddPages(1);
        ActiveLayer = ActivePage.Layers.CreateLayer("Layer 1");
    }

    // AddPages/InsertPages are real IVGDocument methods (confirmed against the manifest
    // and live CorelDRAW) - they belong on the document, not the Pages collection, even
    // though they mutate it; these just forward to it.
    public FakePage AddPages(int count) => Pages.AddPages(count);

    public FakePage InsertPages(int count, bool beforePage, int page) => Pages.InsertPages(count, beforePage, page);

    public void Export(string fileName, int filter, int range, FakeExportOptions options, FakePaletteOptions paletteOptions)
    {
        ExportedPath = fileName;
        ExportedFilter = filter;
        ExportedRange = range;
        ExportedOptions = options;
    }

    public void PublishToPDF(string fileName) => PublishedPdfPath = fileName;

    public void Undo(int levels) => UndoLevels = levels;

    public void Redo(int levels) => RedoLevels = levels;
}

/// <summary>Stand-in for IVGPages: Item(index)/Count only, same shape as the real
/// interface used by `page add/delete/goto/list` (no First/Last needed here since the
/// curated commands only ever index by number).</summary>
public sealed class FakePages
{
    private readonly List<FakePage> _pages = [];

    public Action<FakePage>? OnPageCreated { get; set; }

    public int Count => _pages.Count;

    public FakePage Item(int index) => _pages[index - 1];

    public FakePage AddPages(int count)
    {
        FakePage last = null!;
        for (int i = 0; i < count; i++)
        {
            last = CreateAndInsert(_pages.Count);
        }
        return last;
    }

    /// <summary>Mirrors the real, live-verified Document.InsertPages(NumberOfPages,
    /// BeforePage, Page) signature: `page` is the 1-based reference page index,
    /// `beforePage` picks which side of it the new page(s) land on.</summary>
    public FakePage InsertPages(int count, bool beforePage, int page)
    {
        int insertAt = beforePage ? page - 1 : page;
        FakePage last = null!;
        for (int i = 0; i < count; i++)
        {
            last = CreateAndInsert(insertAt + i);
        }
        return last;
    }

    private FakePage CreateAndInsert(int zeroBasedIndex)
    {
        var page = new FakePage { Name = $"Page {_pages.Count + 1}" };
        page.OnDelete = () => Remove(page);
        _pages.Insert(zeroBasedIndex, page);
        Renumber();
        OnPageCreated?.Invoke(page);
        return page;
    }

    private void Remove(FakePage page)
    {
        _pages.Remove(page);
        Renumber();
    }

    private void Renumber()
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Index = i + 1;
        }
    }
}

public sealed class FakeExportOptions
{
    public bool Overwrite { get; set; }
    public double ResolutionX { get; set; }
    public double ResolutionY { get; set; }
    public int ImageType { get; set; }
    public int AntiAliasingType { get; set; }
    public int Compression { get; set; }
    public bool MaintainLayers { get; set; }
    public bool MaintainAspect { get; set; }
    public bool Transparent { get; set; }
    public bool Dithered { get; set; }
    public bool UseColorProfile { get; set; }
    public double SizeX { get; set; }
    public double SizeY { get; set; }
}

public sealed class FakePaletteOptions;

public sealed class FakePage
{
    // Layers and Shapes share one backing list so that shapes created on ANY layer of
    // this page are visible via ActivePage.Shapes - this is the fix for the real bug
    // (shape list/delete/rename used to only see ActiveLayer) that task 22 verified
    // live and task 24's multi-layer model must not silently regress.
    private readonly List<FakeShape> _shapes = [];

    public FakePage()
    {
        Layers = new FakeLayers(_shapes);
        Shapes = new FakeShapes(_shapes);
    }

    public double SizeWidth { get; set; } = 10;
    public double SizeHeight { get; set; } = 20;
    public bool SetSizeCalled { get; private set; }
    public string Name { get; set; } = "Page";
    public int Index { get; set; } = 1;
    public bool Deleted { get; private set; }
    public bool ActivateCalled { get; private set; }
    public FakeLayers Layers { get; }
    public FakeShapes Shapes { get; }

    internal Action? OnDelete { get; set; }
    internal Action? OnActivate { get; set; }

    public void SetSize(double width, double height)
    {
        SetSizeCalled = true;
        SizeWidth = width;
        SizeHeight = height;
    }

    public void Delete()
    {
        Deleted = true;
        OnDelete?.Invoke();
    }

    public void Activate()
    {
        ActivateCalled = true;
        OnActivate?.Invoke();
    }

    // CreateLayer is a real IVGPage method (confirmed against the manifest and live
    // CorelDRAW) - forwards to the page's own Layers collection.
    public FakeLayer CreateLayer(string layerName) => Layers.CreateLayer(layerName);
}

/// <summary>Stand-in for IVGLayers: Item(index)/Count/Find(name) only - the real interface
/// (confirmed against the generated capability manifest) has no First/Last either. Every
/// layer this creates shares the owning FakePage's single shape list (see FakePage) so
/// shapes created on any layer are visible page-wide, matching real CorelDRAW.</summary>
public sealed class FakeLayers(List<FakeShape> pageShapes)
{
    private readonly List<FakeLayer> _layers = [];

    public Action<FakeLayer>? OnLayerCreated { get; set; }

    public int Count => _layers.Count;

    public FakeLayer Item(int index) => _layers[index - 1];

    public FakeLayer? Find(string name) => _layers.FirstOrDefault(l => l.Name == name);

    public FakeLayer CreateLayer(string name)
    {
        var layer = new FakeLayer(pageShapes) { Name = name, Index = _layers.Count + 1 };
        layer.OnDelete = () => Remove(layer);
        _layers.Add(layer);
        OnLayerCreated?.Invoke(layer);
        return layer;
    }

    private void Remove(FakeLayer layer)
    {
        _layers.Remove(layer);
        Renumber();
    }

    private void Renumber()
    {
        for (int i = 0; i < _layers.Count; i++)
        {
            _layers[i].Index = i + 1;
        }
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
    public FakeLayer() : this([]) { }

    public FakeLayer(List<FakeShape> shapes) => Created = shapes;

    public List<FakeShape> Created { get; }
    public string Name { get; set; } = "Layer";
    public int Index { get; set; } = 1;
    public bool Deleted { get; private set; }
    public bool ActivateCalled { get; private set; }

    internal Action? OnDelete { get; set; }
    internal Action? OnActivate { get; set; }

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

    public void Delete()
    {
        Deleted = true;
        OnDelete?.Invoke();
    }

    public void Activate()
    {
        ActivateCalled = true;
        OnActivate?.Invoke();
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
