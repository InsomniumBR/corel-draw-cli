using Corel.Cli.Commands;

namespace Corel.Cli.Tests;

public class ImageCommandsTests
{
    [Theory]
    [InlineData("photo.png", 802)]
    [InlineData("photo.PNG", 802)]
    [InlineData("photo.jpg", 774)]
    [InlineData("photo.jpeg", 774)]
    [InlineData("photo.tif", 772)]
    [InlineData("photo.tiff", 772)]
    [InlineData("photo.bmp", 769)]
    public void FilterForExtension_MapsKnownExtensions(string file, int expectedFilter)
    {
        Assert.Equal(expectedFilter, ImageCommands.FilterForExtension(file));
    }

    [Fact]
    public void FilterForExtension_ThrowsForUnknownExtension()
    {
        Assert.Throws<ArgumentException>(() => ImageCommands.FilterForExtension("photo.gif"));
    }
}
