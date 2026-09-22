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
    public FakePages Pages { get; }
    public FakePage ActivePage { get; set; }
    public FakeLayer ActiveLayer { get; set; }

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

public sealed class FakePage
{
    public double SizeWidth { get; set; } = 10;
    public double SizeHeight { get; set; } = 20;
    public bool SetSizeCalled { get; private set; }
    public string Name { get; set; } = "Page";
    public int Index { get; set; } = 1;
    public bool Deleted { get; private set; }
    public bool ActivateCalled { get; private set; }
    public FakeLayers Layers { get; } = new();

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
/// (confirmed against the generated capability manifest) has no First/Last either.</summary>
public sealed class FakeLayers
{
    private readonly List<FakeLayer> _layers = [];

    public Action<FakeLayer>? OnLayerCreated { get; set; }

    public int Count => _layers.Count;

    public FakeLayer Item(int index) => _layers[index - 1];

    public FakeLayer? Find(string name) => _layers.FirstOrDefault(l => l.Name == name);

    public FakeLayer CreateLayer(string name)
    {
        var layer = new FakeLayer { Name = name, Index = _layers.Count + 1 };
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

public sealed class FakeLayer
{
    public List<FakeShape> Created { get; } = [];
    public string Name { get; set; } = "Layer";
    public int Index { get; set; } = 1;
    public bool Deleted { get; private set; }
    public bool ActivateCalled { get; private set; }

    internal Action? OnDelete { get; set; }
    internal Action? OnActivate { get; set; }

    public FakeShape CreateRectangle(
        double left, double top, double right, double bottom,
        double cornerTL, double cornerTR, double cornerBR, double cornerBL)
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
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
}
