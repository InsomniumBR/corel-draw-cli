using Corel.Automation;

namespace Corel.Automation.Tests;

public class CorelInstallDetectorTests
{
    [Theory]
    [InlineData(
        "\"p:\\Program Files\\Corel\\CorelDRAW Graphics Suite X7\\Programs64\\CorelDRW.exe\" /Automation",
        "p:\\Program Files\\Corel\\CorelDRAW Graphics Suite X7\\Programs64\\CorelDRW.exe")]
    [InlineData("\"C:\\Corel\\CorelDRW.exe\"", "C:\\Corel\\CorelDRW.exe")]
    [InlineData("C:\\Corel\\CorelDRW.exe /Automation", "C:\\Corel\\CorelDRW.exe")]
    [InlineData("C:\\Corel\\CorelDRW.exe", "C:\\Corel\\CorelDRW.exe")]
    public void ExtractExecutablePath_HandlesQuotedAndUnquotedForms(string raw, string expected)
    {
        Assert.Equal(expected, CorelInstallDetector.ExtractExecutablePath(raw));
    }

    [Fact]
    public void ExtractExecutablePath_ReturnsNullForUnterminatedQuote()
    {
        Assert.Null(CorelInstallDetector.ExtractExecutablePath("\"C:\\Corel\\CorelDRW.exe"));
    }
}
