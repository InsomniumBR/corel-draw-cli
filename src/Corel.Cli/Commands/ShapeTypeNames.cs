using Corel.Schema;

namespace Corel.Cli.Commands;

/// <summary>Resolves a raw cdrShapeType value to its real name via the cached capability manifest, falling back to the raw value if that isn't possible.</summary>
public static class ShapeTypeNames
{
    private const string EnumInterfaceName = "cdrShapeType";

    public static string Resolve(object? rawType, CapabilityManifest? manifest)
    {
        if (rawType is null)
        {
            return "null";
        }

        if (manifest is not null
            && manifest.Interfaces.TryGetValue(EnumInterfaceName, out InterfaceInfo? shapeTypeEnum))
        {
            try
            {
                long value = Convert.ToInt64(rawType);
                foreach ((string name, MemberInfo member) in shapeTypeEnum.Members)
                {
                    if (member.Value == value)
                    {
                        return name;
                    }
                }
            }
            catch
            {
            }
        }

        return rawType.ToString() ?? "null";
    }
}
