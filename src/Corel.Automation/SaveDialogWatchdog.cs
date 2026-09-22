using System.Runtime.InteropServices;
using System.Text;

namespace Corel.Automation;

/// <summary>
/// CorelDRAW's SaveAs was observed to intermittently fall back to an interactive
/// "Save Drawing" file-picker dialog instead of saving headlessly, even with a full
/// path and a valid StructSaveAsOptions object. Root cause not fully understood; this
/// is a pragmatic mitigation, not a hang guarantee - nothing here bounds how long
/// SaveAs itself can take. While it's in flight, a background watcher polls for that
/// dialog on the specific CorelDRAW process being automated (never other windows on
/// the machine) and clicks its default "Save" button so an unattended run doesn't sit
/// stuck behind it.
/// </summary>
public static class SaveDialogWatchdog
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc proc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private const uint BmClick = 0x00F5;

    /// <summary>
    /// Resolves the OS process id of the CorelDRAW instance behind <paramref name="app"/>,
    /// via Application.CorelScriptTools().GetAppHandle() - the only window handle exposed
    /// anywhere on the object model (confirmed against the generated capability
    /// manifest). Used to scope <see cref="RunWithAutoDismiss"/> to that one process
    /// instead of every top-level window on the machine.
    /// </summary>
    public static uint ResolveCorelProcessId(object app)
    {
        object scriptTools = ComInvoker.Call(app, "CorelScriptTools")
            ?? throw new InvalidOperationException("Application.CorelScriptTools() returned null.");
        object handle = ComInvoker.Call(scriptTools, "GetAppHandle")
            ?? throw new InvalidOperationException("CorelScriptTools.GetAppHandle() returned null.");
        var hwnd = new IntPtr(Convert.ToInt64(handle));
        GetWindowThreadProcessId(hwnd, out uint processId);
        return processId;
    }

    /// <summary>
    /// Runs <paramref name="action"/> on the current thread while a background task
    /// polls for a dialog titled <paramref name="dialogTitle"/> owned by
    /// <paramref name="corelProcessId"/>, and clicks its "Save" button if one appears,
    /// then stops watching once <paramref name="action"/> returns (or throws). If the
    /// watcher itself faults, that fault is swallowed rather than replacing whatever
    /// <paramref name="action"/> produced or threw.
    /// </summary>
    public static void RunWithAutoDismiss(uint corelProcessId, Action action, string dialogTitle = "Save Drawing")
        => RunWithBackgroundWatcher(action, token => Watch(corelProcessId, dialogTitle, token));

    /// <summary>
    /// Runs <paramref name="action"/> while <paramref name="watch"/> runs concurrently
    /// on a background task, cancelling and awaiting the watcher once
    /// <paramref name="action"/> completes. Split out from <see cref="RunWithAutoDismiss"/>
    /// so the "never let the watcher mask the real result" behavior is testable without
    /// a live window/dialog.
    /// </summary>
    internal static void RunWithBackgroundWatcher(Action action, Action<CancellationToken> watch)
    {
        using var stop = new CancellationTokenSource();
        Task watcher = Task.Run(() => watch(stop.Token));
        try
        {
            action();
        }
        finally
        {
            stop.Cancel();
            try
            {
                watcher.Wait();
            }
            catch
            {
                // A fault in the watcher itself is never as important as whatever
                // `action` just did - swallowing it here stops its AggregateException
                // from replacing (and hiding) action's own exception during unwind.
            }
        }
    }

    private static void Watch(uint corelProcessId, string title, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            IntPtr dialog = FindWindowByExactTitle(corelProcessId, title);
            if (dialog != IntPtr.Zero)
            {
                IntPtr saveButton = FindChildButtonByText(dialog, "Save");
                if (saveButton != IntPtr.Zero)
                {
                    SendMessage(saveButton, BmClick, IntPtr.Zero, IntPtr.Zero);
                }
            }
            token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(300));
        }
    }

    private static IntPtr FindWindowByExactTitle(uint ownerProcessId, string title)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out uint windowProcessId);
            if (windowProcessId != ownerProcessId)
            {
                return true;
            }

            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString() == title)
            {
                found = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static IntPtr FindChildButtonByText(IntPtr parent, string text)
    {
        IntPtr found = IntPtr.Zero;
        EnumChildWindows(parent, (hWnd, _) =>
        {
            var cls = new StringBuilder(256);
            GetClassName(hWnd, cls, cls.Capacity);
            if (cls.ToString() != "Button")
            {
                return true;
            }

            var buttonText = new StringBuilder(256);
            GetWindowText(hWnd, buttonText, buttonText.Capacity);
            if (buttonText.ToString() == text)
            {
                found = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
