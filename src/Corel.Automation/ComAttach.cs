namespace Corel.Automation;

/// <summary>
/// Attaches to a running CorelDRAW instance via CoCreateInstance.
/// </summary>
/// <remarks>
/// CorelDRAW never registers itself in the Running Object Table (verified empirically,
/// including with two live processes running simultaneously), so Marshal.GetActiveObject
/// is not viable here. CoCreateInstance (Type.GetTypeFromProgID + Activator.CreateInstance)
/// reliably routes to the already-running primary instance instead, regardless of window
/// focus. A second CorelDRAW process is invisible to COM entirely - only the primary
/// instance is ever reachable this way.
/// </remarks>
public static class ComAttach
{
    public const string DefaultProgId = "CorelDRAW.Application";

    public static object Attach(string progId = DefaultProgId)
    {
        Type type = Type.GetTypeFromProgID(progId)
            ?? throw new InvalidOperationException(
                $"ProgID '{progId}' is not registered on this machine. Is CorelDRAW installed?");

        object? instance = Activator.CreateInstance(type);
        return instance
            ?? throw new InvalidOperationException(
                $"Activator.CreateInstance returned null for ProgID '{progId}'.");
    }
}
