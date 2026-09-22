namespace Corel.Automation;

/// <summary>
/// Maps human-friendly unit names to their cdrUnit enum value. Values match the ground
/// truth scanned from CorelDRAW X7's type library (see TypeLibScanner/M2) and physically
/// verified (1 inch = 25.4mm = 72pt).
/// </summary>
public static class UnitNames
{
    private static readonly Dictionary<string, int> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["in"] = 1,
        ["inch"] = 1,
        ["inches"] = 1,
        ["ft"] = 2,
        ["foot"] = 2,
        ["feet"] = 2,
        ["mm"] = 3,
        ["millimeter"] = 3,
        ["millimeters"] = 3,
        ["cm"] = 4,
        ["centimeter"] = 4,
        ["centimeters"] = 4,
        ["px"] = 5,
        ["pixel"] = 5,
        ["pixels"] = 5,
        ["mi"] = 6,
        ["mile"] = 6,
        ["m"] = 7,
        ["meter"] = 7,
        ["meters"] = 7,
        ["km"] = 8,
        ["kilometer"] = 8,
        ["yd"] = 11,
        ["yard"] = 11,
        ["pica"] = 12,
        ["pt"] = 14,
        ["point"] = 14,
        ["points"] = 14,
    };

    public static int ToEnumValue(string name)
        => Map.TryGetValue(name, out int value)
            ? value
            : throw new ArgumentException($"Unknown unit '{name}'. Known units: {string.Join(", ", Map.Keys)}.");
}
