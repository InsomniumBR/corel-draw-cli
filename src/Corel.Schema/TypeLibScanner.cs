using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace Corel.Schema;

/// <summary>
/// Walks a CorelDRAW automation type library (vgcoreauto.tlb) via ITypeInfo and produces
/// a <see cref="CapabilityManifest"/> - the ground truth for what a given CorelDRAW
/// version's object model actually exposes, since guessing member names/signatures
/// against the live app has repeatedly proven wrong (e.g. "AddRectangle" doesn't exist;
/// the real member is Layer.CreateRectangle).
/// </summary>
/// <remarks>
/// Needs only the .tlb file path - does not require a running CorelDRAW instance, and
/// therefore does not need any live COM attach.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class TypeLibScanner
{
    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void LoadTypeLib(string szFile, out ITypeLib pptlib);

    public static CapabilityManifest Scan(string tlbPath, string corelVersion, string progId)
    {
        LoadTypeLib(tlbPath, out ITypeLib lib);

        var manifest = new CapabilityManifest
        {
            CorelVersion = corelVersion,
            ProgId = progId,
            TlbPath = tlbPath,
            GeneratedAtUtc = DateTime.UtcNow,
        };

        int count = lib.GetTypeInfoCount();
        for (int i = 0; i < count; i++)
        {
            lib.GetDocumentation(i, out string name, out _, out _, out _);
            lib.GetTypeInfo(i, out ITypeInfo info);

            info.GetTypeAttr(out IntPtr pAttr);
            try
            {
                TYPEATTR attr = Marshal.PtrToStructure<TYPEATTR>(pAttr);
                InterfaceInfo? scanned = attr.typekind switch
                {
                    TYPEKIND.TKIND_ENUM => ScanEnum(info, attr),
                    TYPEKIND.TKIND_DISPATCH or TYPEKIND.TKIND_INTERFACE => ScanMembers(info, attr),
                    _ => null,
                };
                if (scanned is not null)
                {
                    manifest.Interfaces[name] = scanned;
                }
            }
            finally
            {
                info.ReleaseTypeAttr(pAttr);
            }
        }

        return manifest;
    }

    private static InterfaceInfo ScanEnum(ITypeInfo info, TYPEATTR attr)
    {
        var result = new InterfaceInfo { Kind = "enum" };
        for (int i = 0; i < attr.cVars; i++)
        {
            info.GetVarDesc(i, out IntPtr pVarDesc);
            try
            {
                VARDESC vd = Marshal.PtrToStructure<VARDESC>(pVarDesc);
                if (vd.varkind != VARKIND.VAR_CONST)
                {
                    continue;
                }

                string[] names = new string[1];
                info.GetNames(vd.memid, names, 1, out int cNames);
                if (cNames == 0 || names[0] is null)
                {
                    continue;
                }

                long? value = null;
                try
                {
                    object? boxed = Marshal.GetObjectForNativeVariant(vd.desc.lpvarValue);
                    value = Convert.ToInt64(boxed);
                }
                catch
                {
                    // leave value null if it isn't cleanly convertible - not expected for cdrUnit-style enums
                }

                result.Members[names[0]] = new MemberInfo { Kind = "const", Value = value };
            }
            finally
            {
                info.ReleaseVarDesc(pVarDesc);
            }
        }
        return result;
    }

    private static InterfaceInfo ScanMembers(ITypeInfo info, TYPEATTR attr)
    {
        var result = new InterfaceInfo { Kind = "dispatch" };
        for (int f = 0; f < attr.cFuncs; f++)
        {
            info.GetFuncDesc(f, out IntPtr pFuncDesc);
            try
            {
                FUNCDESC fd = Marshal.PtrToStructure<FUNCDESC>(pFuncDesc);
                string[] names = new string[16];
                info.GetNames(fd.memid, names, names.Length, out int cNames);
                if (cNames == 0 || names[0] is null)
                {
                    continue;
                }

                string kind = fd.invkind is INVOKEKIND.INVOKE_PROPERTYGET
                    or INVOKEKIND.INVOKE_PROPERTYPUT
                    or INVOKEKIND.INVOKE_PROPERTYPUTREF
                    ? "property"
                    : "method";

                var paramNames = names.Skip(1).Take(cNames - 1).Where(n => n is not null).ToList();

                // A dispinterface can declare both a getter and setter with the same
                // memid/name (e.g. a property with get+put) - keep the richer entry
                // (the one with more captured param names) rather than overwrite it.
                if (!result.Members.TryGetValue(names[0], out MemberInfo? existing)
                    || paramNames.Count > existing.Params.Count)
                {
                    result.Members[names[0]] = new MemberInfo { Kind = kind, Params = paramNames! };
                }
            }
            finally
            {
                info.ReleaseFuncDesc(pFuncDesc);
            }
        }
        return result;
    }
}
