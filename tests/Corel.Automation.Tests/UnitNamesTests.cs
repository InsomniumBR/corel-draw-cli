namespace Corel.Automation.Tests;

public class UnitNamesTests
{
    [Theory]
    [InlineData("in", 1)]
    [InlineData("inch", 1)]
    [InlineData("inches", 1)]
    [InlineData("ft", 2)]
    [InlineData("foot", 2)]
    [InlineData("feet", 2)]
    [InlineData("mm", 3)]
    [InlineData("millimeter", 3)]
    [InlineData("millimeters", 3)]
    [InlineData("cm", 4)]
    [InlineData("centimeter", 4)]
    [InlineData("centimeters", 4)]
    [InlineData("px", 5)]
    [InlineData("pixel", 5)]
    [InlineData("pixels", 5)]
    [InlineData("mi", 6)]
    [InlineData("mile", 6)]
    [InlineData("m", 7)]
    [InlineData("meter", 7)]
    [InlineData("meters", 7)]
    [InlineData("km", 8)]
    [InlineData("kilometer", 8)]
    [InlineData("yd", 11)]
    [InlineData("yard", 11)]
    [InlineData("pica", 12)]
    [InlineData("pt", 14)]
    [InlineData("point", 14)]
    [InlineData("points", 14)]
    public void ToEnumValue_MapsKnownUnitNameToItsCdrUnitValue(string name, int expected)
    {
        Assert.Equal(expected, UnitNames.ToEnumValue(name));
    }

    [Theory]
    [InlineData("IN")]
    [InlineData("In")]
    [InlineData("MM")]
    [InlineData("Point")]
    public void ToEnumValue_IsCaseInsensitive(string name)
    {
        // Doesn't assert a specific value here - just that casing alone never causes the
        // "unknown unit" failure path.
        int value = UnitNames.ToEnumValue(name);
        Assert.Equal(UnitNames.ToEnumValue(name.ToLowerInvariant()), value);
    }

    [Fact]
    public void ToEnumValue_ThrowsArgumentException_ForUnknownUnit()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => UnitNames.ToEnumValue("furlong"));
        Assert.Contains("furlong", ex.Message);
    }

    [Fact]
    public void ToEnumValue_ExceptionMessage_ListsKnownUnits()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => UnitNames.ToEnumValue("furlong"));
        Assert.Contains("in", ex.Message);
        Assert.Contains("mm", ex.Message);
        Assert.Contains("pt", ex.Message);
    }
}
