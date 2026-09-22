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
    public FakePage ActivePage { get; set; } = new();
    public FakeLayer ActiveLayer { get; set; } = new();
    public string? ExportedPath { get; private set; }
    public int? ExportedFilter { get; private set; }
    public int? ExportedRange { get; private set; }
    public FakeExportOptions? ExportedOptions { get; private set; }
    public string? PublishedPdfPath { get; private set; }
    public int UndoLevels { get; private set; }
    public int RedoLevels { get; private set; }

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
