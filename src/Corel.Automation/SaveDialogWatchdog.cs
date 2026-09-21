using System.Runtime.InteropServices;
using System.Text;

namespace Corel.Automation;

/// <summary>
/// CorelDRAW's SaveAs was observed to intermittently fall back to an interactive
/// "Save Drawing" file-picker dialog instead of saving headlessly, even with a full
/// path and a valid StructSaveAsOptions object - reproduced against a real,
/// message-pumping STA thread, so it isn't simply the calling thread failing to pump
/// messages (that fix is still correct and kept, but doesn't fully explain this).
/// Root cause not fully understood; this is a pragmatic safety net so it can never
/// hang the CLI indefinitely: while the SaveAs call is in flight, a background watcher
/// polls for that dialog and clicks its default "Save" button automatically.
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

    private const uint BmClick = 0x00F5;

    /// <summary>
    /// Runs <paramref name="action"/> on the current thread while a background task
    /// polls for a dialog titled <paramref name="dialogTitle"/> and clicks its "Save"
    /// button if one appears, then stops watching once <paramref name="action"/>
    /// returns (or throws).
    /// </summary>
    public static void RunWithAutoDismiss(Action action, string dialogTitle = "Save Drawing")
    {
        using var stop = new CancellationTokenSource();
        Task watcher = Task.Run(() => Watch(dialogTitle, stop.Token));
        try
        {
            action();
        }
        finally
        {
            stop.Cancel();
            watcher.Wait();
        }
    }

    private static void Watch(string title, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            IntPtr dialog = FindWindowByExactTitle(title);
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

    private static IntPtr FindWindowByExactTitle(string title)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, _) =>
        {
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
