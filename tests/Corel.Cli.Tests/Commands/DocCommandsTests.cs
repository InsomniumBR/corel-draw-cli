using Corel.Cli.Commands;

namespace Corel.Cli.Tests.Commands;

public class DocCommandsTests
{
    private static string ExistingDir => Path.GetTempPath();

    [Fact]
    public void Export_MissingRequiredFlags_ReturnsUsageError()
    {
        var app = new FakeApplication();
        string[] args = ["export", "--as", Path.Combine(ExistingDir, "out.png")];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(1, result);
        Assert.Null(app.ActiveDocument.ExportedPath);
    }

    [Fact]
    public void Export_UnsupportedFormat_ReturnsErrorWithoutExporting()
    {
        var app = new FakeApplication();
        string[] args = ["export", "--as", Path.Combine(ExistingDir, "out.bmp"), "--format", "bmp"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(1, result);
        Assert.Null(app.ActiveDocument.ExportedPath);
    }

    [Fact]
    public void Export_TargetDirectoryMissing_ReturnsErrorWithoutExporting()
    {
        var app = new FakeApplication();
        string missingDir = Path.Combine(ExistingDir, Guid.NewGuid().ToString());
        string[] args = ["export", "--as", Path.Combine(missingDir, "out.png"), "--format", "png"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(1, result);
        Assert.Null(app.ActiveDocument.ExportedPath);
    }

    [Fact]
    public void Export_Pdf_CallsPublishToPDF_NotTheGenericExportPath()
    {
        var app = new FakeApplication();
        string path = Path.Combine(ExistingDir, "out.pdf");
        string[] args = ["export", "--as", path, "--format", "pdf"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(path, app.ActiveDocument.PublishedPdfPath);
        Assert.Null(app.ActiveDocument.ExportedPath);
    }

    [Fact]
    public void Export_Png_UsesExportWithCdrPngFilterAndDefaultOptions()
    {
        var app = new FakeApplication();
        string path = Path.Combine(ExistingDir, "out.png");
        string[] args = ["export", "--as", path, "--format", "png"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(path, app.ActiveDocument.ExportedPath);
        Assert.Equal(802, app.ActiveDocument.ExportedFilter); // cdrPNG
        Assert.NotNull(app.ActiveDocument.ExportedOptions);
        Assert.True(app.ActiveDocument.ExportedOptions!.Overwrite);
        Assert.Equal(300d, app.ActiveDocument.ExportedOptions.ResolutionX);
        Assert.Equal(300d, app.ActiveDocument.ExportedOptions.ResolutionY);
        Assert.True(app.ActiveDocument.ExportedOptions.MaintainAspect);
        Assert.Equal(0d, app.ActiveDocument.ExportedOptions.SizeX); // left at 0 (auto) - no --width given
        Assert.Equal(0d, app.ActiveDocument.ExportedOptions.SizeY);
    }

    [Fact]
    public void Export_Jpeg_UsesExportWithCdrJpegFilter()
    {
        var app = new FakeApplication();
        string path = Path.Combine(ExistingDir, "out.jpg");
        string[] args = ["export", "--as", path, "--format", "jpg"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(774, app.ActiveDocument.ExportedFilter); // cdrJPEG
    }

    [Fact]
    public void Export_Svg_UsesExportWithCdrSvgFilter()
    {
        var app = new FakeApplication();
        string path = Path.Combine(ExistingDir, "out.svg");
        string[] args = ["export", "--as", path, "--format", "svg"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(1345, app.ActiveDocument.ExportedFilter); // cdrSVG
    }

    [Fact]
    public void Export_WithDpi_OverridesDefaultResolution()
    {
        var app = new FakeApplication();
        string path = Path.Combine(ExistingDir, "out.png");
        string[] args = ["export", "--as", path, "--format", "png", "--dpi", "150"];

        DocCommands.Run(args, app, null);

        Assert.Equal(150d, app.ActiveDocument.ExportedOptions!.ResolutionX);
        Assert.Equal(150d, app.ActiveDocument.ExportedOptions.ResolutionY);
    }

    [Fact]
    public void Export_WithWidthAndHeight_SetsSizeXAndSizeY()
    {
        var app = new FakeApplication();
        string path = Path.Combine(ExistingDir, "out.png");
        string[] args = ["export", "--as", path, "--format", "png", "--width", "800", "--height", "600"];

        DocCommands.Run(args, app, null);

        Assert.Equal(800d, app.ActiveDocument.ExportedOptions!.SizeX);
        Assert.Equal(600d, app.ActiveDocument.ExportedOptions.SizeY);
    }

    [Fact]
    public void Undo_NoLevelsFlag_DefaultsToOne()
    {
        var app = new FakeApplication();
        string[] args = ["undo"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(1, app.ActiveDocument.UndoLevels);
    }

    [Fact]
    public void Undo_WithLevels_PassesThemThrough()
    {
        var app = new FakeApplication();
        string[] args = ["undo", "--levels", "3"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(3, app.ActiveDocument.UndoLevels);
    }

    [Fact]
    public void Redo_NoLevelsFlag_DefaultsToOne()
    {
        var app = new FakeApplication();
        string[] args = ["redo"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(1, app.ActiveDocument.RedoLevels);
    }

    [Fact]
    public void Redo_WithLevels_PassesThemThrough()
    {
        var app = new FakeApplication();
        string[] args = ["redo", "--levels", "2"];

        int result = DocCommands.Run(args, app, null);

        Assert.Equal(0, result);
        Assert.Equal(2, app.ActiveDocument.RedoLevels);
    }
}
