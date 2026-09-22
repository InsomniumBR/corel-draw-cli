using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace Corel.Automation;

/// <summary>Resolves a COM object's real interface name, since .NET's own RCW type is always the generic "System.__ComObject".</summary>
[SupportedOSPlatform("windows")]
public static class ComTypeName
{
    // Declares only the two IDispatch vtable slots used - COM interop matches declared members to vtable slots in order after IUnknown's three, so a partial interface is enough.
    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDispatchTypeInfo
    {
        void GetTypeInfoCount(out int count);

        void GetTypeInfo(int index, int lcid, out ITypeInfo typeInfo);
    }

    /// <summary>Returns the COM object's type name (e.g. "IVGShape"), or null if it isn't resolvable.</summary>
    public static string? TryResolve(object? value)
    {
        if (value is null || !Marshal.IsComObject(value) || value is not IDispatchTypeInfo dispatch)
        {
            return null;
        }

        try
        {
            dispatch.GetTypeInfoCount(out int count);
            if (count == 0)
            {
                return null;
            }

            dispatch.GetTypeInfo(0, 0, out ITypeInfo typeInfo);
            typeInfo.GetDocumentation(-1, out string? name, out _, out _, out _);
            return name;
        }
        catch
        {
            return null;
        }
    }
}
