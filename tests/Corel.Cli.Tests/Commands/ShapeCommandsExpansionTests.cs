using Corel.Cli.Commands;
using Corel.Schema;

namespace Corel.Cli.Tests.Commands;

[Collection("console")]
public class ShapeCommandsExpansionTests
{
    private static int Run(FakeApplication app, params string[] args)
    {
        using var capture = new ConsoleCapture();
        return ShapeCommands.Run(args, app, null, TestManifest.Build());
    }

    private static FakeApplication WithShapes(int count)
    {
        var app = new FakeApplication();
        for (int i = 0; i < count; i++)
        {
            app.ActiveDocument.ActiveLayer.CreateRectangle(i, i + 1, i + 1, i, 0, 0, 0, 0);
        }
        return app;
    }

    [Fact]
    public void CreateEllipse_ValidArgs_CallsTheEllipseFactory()
    {
        var app = new FakeApplication();

        int result = Run(app, "create-ellipse", "--left", "0", "--top", "2", "--right", "2", "--bottom", "0", "--pie");

        Assert.Equal(0, result);
        Assert.Contains("CreateEllipse(pie=True", app.ActiveDocument.ActiveLayer.Calls[0]);
    }

    [Fact]
    public void CreatePolygon_WithoutSides_IsAUsageErrorAndCreatesNothing()
    {
        var app = new FakeApplication();

        int result = Run(app, "create-polygon", "--left", "0", "--top", "2", "--right", "2", "--bottom", "0");

        Assert.Equal(1, result);
        Assert.Empty(app.ActiveDocument.ActiveLayer.Created);
    }

    [Fact]
    public void CreatePolygon_FewerThanThreeSides_IsRejected()
    {
        var app = new FakeApplication();

        int result = Run(app, "create-polygon", "--left", "0", "--top", "2", "--right", "2", "--bottom", "0", "--sides", "2");

        Assert.Equal(1, result);
        Assert.Empty(app.ActiveDocument.ActiveLayer.Created);
    }

    [Fact]
    public void CreateLine_ValidArgs_CallsTheLineFactory()
    {
        var app = new FakeApplication();

        int result = Run(app, "create-line", "--x1", "0", "--y1", "0", "--x2", "3", "--y2", "4");

        Assert.Equal(0, result);
        Assert.Equal("CreateLineSegment(0,0,3,4)", app.ActiveDocument.ActiveLayer.Calls[0]);
    }

    [Fact]
    public void CreateText_DefaultsToArtistic()
    {
        var app = new FakeApplication();

        int result = Run(app, "create-text", "--text", "hi", "--left", "1", "--bottom", "1", "--font", "Arial", "--size", "12");

        Assert.Equal(0, result);
        Assert.Equal("CreateArtisticText('hi',font=Arial,size=12,bold=)", app.ActiveDocument.ActiveLayer.Calls[0]);
    }

    [Fact]
    public void CreateText_Paragraph_NeedsTheWholeFrame()
    {
        var app = new FakeApplication();

        int result = Run(app, "create-text", "--text", "hi", "--paragraph", "--left", "1", "--bottom", "1");

        Assert.Equal(1, result);
        Assert.Empty(app.ActiveDocument.ActiveLayer.Calls);
    }

    [Fact]
    public void CreateText_ParagraphWithFrame_CallsTheParagraphFactory()
    {
        var app = new FakeApplication();

        int result = Run(
            app, "create-text", "--text", "hi", "--paragraph",
            "--left", "1", "--bottom", "1", "--top", "3", "--right", "4", "--bold");

        Assert.Equal(0, result);
        Assert.Contains("CreateParagraphText('hi'", app.ActiveDocument.ActiveLayer.Calls[0]);
        Assert.Contains("bold=True", app.ActiveDocument.ActiveLayer.Calls[0]);
    }

    [Fact]
    public void List_IsPageWideNotActiveLayerOnly()
    {
        FakeApplication app = WithShapes(3);
        app.ActiveDocument.ActiveLayer = new FakeLayer();

        using var capture = new ConsoleCapture();
        int result = ShapeCommands.Run(["list"], app, null, null);

        Assert.Equal(0, result);
        Assert.Contains("[3]", capture.Out);
    }

    [Fact]
    public void Move_Relative_CallsMove()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "move", "--index", "1", "--dx", "2", "--dy", "-1");

        Assert.Equal(0, result);
        Assert.Equal("Move(2,-1)", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void Move_Absolute_CallsSetPosition()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "move", "--index", "1", "--x", "4", "--y", "5");

        Assert.Equal(0, result);
        Assert.Equal("SetPosition(4,5)", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void Move_BothOffsetAndAbsolute_IsAUsageError()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "move", "--index", "1", "--dx", "1", "--x", "4");

        Assert.Equal(1, result);
        Assert.Empty(app.ActiveDocument.ActivePage.Shapes.Item(1).Operations);
    }

    [Fact]
    public void Move_OutOfRangeIndex_SaysHowManyShapesThereAre()
    {
        FakeApplication app = WithShapes(2);

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Run(app, "move", "--index", "7", "--dx", "1"));

        Assert.Contains("the active page has 2 shape(s)", error.Message);
    }

    [Fact]
    public void Rotate_WithOnlyOneCentreCoordinate_IsAUsageError()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "rotate", "--index", "1", "--degrees", "30", "--center-x", "2");

        Assert.Equal(1, result);
        Assert.Empty(app.ActiveDocument.ActivePage.Shapes.Item(1).Operations);
    }

    [Fact]
    public void Rotate_WithBothCentreCoordinates_UsesRotateEx()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "rotate", "--index", "1", "--degrees", "30", "--center-x", "2", "--center-y", "3");

        Assert.Equal(0, result);
        Assert.Equal("RotateEx(30,2,3)", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void Scale_ByFactor_Stretches()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "scale", "--index", "1", "--factor", "1.5");

        Assert.Equal(0, result);
        Assert.Equal("Stretch(1.5,1.5)", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void Scale_BySizeAndFactorTogether_IsAUsageError()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "scale", "--index", "1", "--width", "2", "--height", "2", "--factor", "1.5");

        Assert.Equal(1, result);
        Assert.Empty(app.ActiveDocument.ActivePage.Shapes.Item(1).Operations);
    }

    [Fact]
    public void Flip_ResolvesTheAxisNameFromTheManifest()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "flip", "--index", "1", "--axis", "vertical");

        Assert.Equal(0, result);
        Assert.Equal("Flip(2)", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void Flip_UnknownAxis_NamesTheAcceptedWords()
    {
        FakeApplication app = WithShapes(1);

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Run(app, "flip", "--index", "1", "--axis", "sideways"));

        Assert.Contains("horizontal, vertical, both", error.Message);
    }

    [Fact]
    public void Flip_WithoutAManifest_SaysToGenerateOne()
    {
        FakeApplication app = WithShapes(1);

        using var capture = new ConsoleCapture();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => ShapeCommands.Run(["flip", "--index", "1", "--axis", "both"], app, null, null));

        Assert.Contains("corel schema generate", error.Message);
    }

    [Fact]
    public void Duplicate_DefaultsToNoOffset()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "duplicate", "--index", "1");

        Assert.Equal(0, result);
        Assert.Equal("Duplicate(0,0)", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Theory]
    [InlineData("front", "OrderToFront")]
    [InlineData("back", "OrderToBack")]
    [InlineData("forward", "OrderForwardOne")]
    [InlineData("backward", "OrderBackOne")]
    public void Order_MapsEachWordToItsOwnMethod(string word, string method)
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "order", "--index", "1", "--to", word);

        Assert.Equal(0, result);
        Assert.Equal(method, app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void Order_UnknownDirection_IsAUsageError()
    {
        FakeApplication app = WithShapes(1);

        Assert.Equal(1, Run(app, "order", "--index", "1", "--to", "sideways"));
    }

    [Fact]
    public void Group_All_GroupsEveryShapeOnThePage()
    {
        FakeApplication app = WithShapes(3);

        using var capture = new ConsoleCapture();
        int result = ShapeCommands.Run(["group", "--all"], app, null, null);

        Assert.Equal(0, result);
        Assert.Contains("Grouped 3 shape(s)", capture.Out);
    }

    [Fact]
    public void Group_NeitherAllNorIndexes_IsRejected()
    {
        FakeApplication app = WithShapes(3);

        ArgumentException error = Assert.Throws<ArgumentException>(() => Run(app, "group"));

        Assert.Contains("exactly one of --all or --indexes", error.Message);
    }

    [Fact]
    public void Group_BothAllAndIndexes_IsRejected()
    {
        FakeApplication app = WithShapes(3);

        Assert.Throws<ArgumentException>(() => Run(app, "group", "--all", "--indexes", "1,2"));
    }

    [Fact]
    public void Ungroup_AllLevels_UsesUngroupAll()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "ungroup", "--index", "1", "--all-levels");

        Assert.Equal(0, result);
        Assert.Equal("UngroupAll", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void BreakApart_CallsBreakApart()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "break-apart", "--index", "1");

        Assert.Equal(0, result);
        Assert.Equal("BreakApart", app.ActiveDocument.ActivePage.Shapes.Item(1).Operations[0]);
    }

    [Fact]
    public void Align_DefaultsToAligningTheShapesToTheFirstOfThem()
    {
        FakeApplication app = WithShapes(3);

        using var capture = new ConsoleCapture();
        int result = ShapeCommands.Run(
            ["align", "--indexes", "1,2", "--horizontal", "left"], app, null, TestManifest.Build());

        Assert.Equal(0, result);
        Assert.Contains("to the first of them", capture.Out);
    }

    [Fact]
    public void Align_ToPage_MovesTheWholeBlock()
    {
        FakeApplication app = WithShapes(3);

        using var capture = new ConsoleCapture();
        int result = ShapeCommands.Run(
            ["align", "--all", "--horizontal", "right", "--vertical", "top", "--to", "page"],
            app, null, TestManifest.Build());

        Assert.Equal(0, result);
        Assert.Contains("as one block to the page", capture.Out);
    }

    [Fact]
    public void Align_CombinesTheHorizontalAndVerticalEnumValuesIntoOneArgument()
    {
        FakeApplication app = WithShapes(3);

        int result = Run(app, "align", "--all", "--horizontal", "left", "--vertical", "top");

        Assert.Equal(0, result);
        Assert.Equal(
            "AlignToShape(5,FakeShape)",
            app.ActiveDocument.ActivePage.Shapes.LastRange!.Operations[0]);
    }

    [Fact]
    public void Align_ToPageCentre_UsesTheRangeLevelMember()
    {
        FakeApplication app = WithShapes(2);

        int result = Run(app, "align", "--indexes", "1,2", "--vertical", "center", "--to", "page-center");

        Assert.Equal(0, result);
        Assert.Equal(
            "AlignRangeToPageCenter(12)",
            app.ActiveDocument.ActivePage.Shapes.LastRange!.Operations[0]);
    }

    [Fact]
    public void Align_WithoutAnAxis_IsAUsageError()
    {
        FakeApplication app = WithShapes(2);

        Assert.Equal(1, Run(app, "align", "--all", "--to", "page"));
    }

    [Fact]
    public void Align_UnknownTarget_IsAUsageError()
    {
        FakeApplication app = WithShapes(2);

        Assert.Equal(1, Run(app, "align", "--all", "--horizontal", "left", "--to", "moon"));
    }

    [Fact]
    public void Distribute_OverPage_PassesTheFlagThrough()
    {
        FakeApplication app = WithShapes(3);

        int result = Run(app, "distribute", "--all", "--horizontal", "spacing", "--over-page");

        Assert.Equal(0, result);
        Assert.Equal("Distribute(4,True)", app.ActiveDocument.ActivePage.Shapes.LastRange!.Operations[0]);
    }

    [Fact]
    public void Distribute_WithoutAnAxis_IsAUsageError()
    {
        FakeApplication app = WithShapes(3);

        Assert.Equal(1, Run(app, "distribute", "--all"));
    }

    [Fact]
    public void Fill_AssignsTheColourComponents()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "fill", "--index", "1", "--color", "FF8000");

        Assert.Equal(0, result);
        FakeColor color = app.ActiveDocument.ActivePage.Shapes.Item(1).Fill.UniformColor;
        Assert.Equal(255, color.Red);
        Assert.Equal(128, color.Green);
        Assert.Equal(0, color.Blue);
    }

    [Fact]
    public void Fill_None_RemovesTheFill()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "fill", "--index", "1", "--none");

        Assert.Equal(0, result);
        Assert.True(app.ActiveDocument.ActivePage.Shapes.Item(1).Fill.NoFillApplied);
    }

    [Fact]
    public void Fill_ColourAndNoneTogether_IsAUsageError()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "fill", "--index", "1", "--color", "FF0000", "--none");

        Assert.Equal(1, result);
        Assert.Null(app.ActiveDocument.ActivePage.Shapes.Item(1).Fill.UniformColor.Red);
    }

    [Fact]
    public void Outline_SetsWidthAndColour()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "outline", "--index", "1", "--width", "0.05", "--color", "#0000FF");

        Assert.Equal(0, result);
        FakeOutline outline = app.ActiveDocument.ActivePage.Shapes.Item(1).Outline;
        Assert.Equal(0.05, outline.Width);
        Assert.Equal(255, outline.Color.Blue);
    }

    [Fact]
    public void Outline_None_RemovesTheOutline()
    {
        FakeApplication app = WithShapes(1);

        int result = Run(app, "outline", "--index", "1", "--none");

        Assert.Equal(0, result);
        Assert.True(app.ActiveDocument.ActivePage.Shapes.Item(1).Outline.NoOutlineApplied);
    }

    [Fact]
    public void Outline_NeitherWidthNorColourNorNone_IsAUsageError()
    {
        FakeApplication app = WithShapes(1);

        Assert.Equal(1, Run(app, "outline", "--index", "1"));
    }
}

internal static class TestManifest
{
    /// <summary>The enum values this project reads out of a real manifest, as the X7 type library
    /// actually defines them - so a test failure means the lookup broke, not that a number drifted.</summary>
    public static CapabilityManifest Build() => new()
    {
        CorelVersion = "17",
        ProgId = "CorelDRAW.Application.17",
        TlbPath = "test",
        Interfaces = new Dictionary<string, InterfaceInfo>
        {
            ["cdrFlipAxes"] = Enum(("cdrFlipHorizontal", 1), ("cdrFlipVertical", 2), ("cdrFlipBoth", 3)),
            ["cdrAlignType"] = Enum(
                ("cdrAlignLeft", 1), ("cdrAlignRight", 2), ("cdrAlignHCenter", 3),
                ("cdrAlignTop", 4), ("cdrAlignBottom", 8), ("cdrAlignVCenter", 12)),
            ["cdrDistributeType"] = Enum(
                ("cdrDistributeLeft", 1), ("cdrDistributeRight", 2), ("cdrDistributeHCenter", 3),
                ("cdrDistributeHSpacing", 4), ("cdrDistributeTop", 8), ("cdrDistributeBottom", 16),
                ("cdrDistributeVCenter", 24), ("cdrDistributeVSpacing", 32)),
        },
    };

    private static InterfaceInfo Enum(params (string Name, long Value)[] members) => new()
    {
        Kind = "TKIND_ENUM",
        Members = members.ToDictionary(
            m => m.Name,
            m => new Corel.Schema.MemberInfo { Kind = "const", Value = m.Value }),
    };
}
