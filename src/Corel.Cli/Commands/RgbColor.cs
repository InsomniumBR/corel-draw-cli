using System.Globalization;

namespace Corel.Cli.Commands;

public readonly record struct RgbColor(int Red, int Green, int Blue)
{
    public static RgbColor Parse(string value)
    {
        string hex = value.StartsWith('#') ? value[1..] : value;
        if (hex.Length != 6
            || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int packed))
        {
            throw new ArgumentException($"'{value}' is not a colour: expected 6 hex digits, e.g. --color FF8800 or #FF8800.");
        }

        return new RgbColor((packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
    }

    public override string ToString()
        => $"{Red:X2}{Green:X2}{Blue:X2}";
}
