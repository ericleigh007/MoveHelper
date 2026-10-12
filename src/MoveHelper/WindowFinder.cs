using System.Diagnostics;

namespace MoveHelper;

internal readonly record struct WindowSample(
    IntPtr Hwnd,
    string ProcessName,
    string ClassName,
    Rectangle Frame,
    Rectangle Caption,
    Rectangle? Buttons);

internal static class WindowFinder
{
    private static readonly HashSet<string> ShellClasses = new(StringComparer.Ordinal)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "NotifyIconOverflowWindow",
        "TopLevelWindowForOverflowXamlIsland",
        "XamlExplorerHostIslandWindow",
    };

    public static IntPtr RootUnderCursor(int ownProcessId)
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return IntPtr.Zero;
        }

        var hwnd = NativeMethods.WindowFromPoint(point);
        if (hwnd == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        if (hwnd == IntPtr.Zero || IsOwnProcess(hwnd, ownProcessId))
        {
            return IntPtr.Zero;
        }

        return hwnd;
    }

    public static WindowSample? Describe(IntPtr hwnd, int ownProcessId, IReadOnlySet<string> excludedProcesses)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd) || NativeMethods.IsIconic(hwnd))
        {
            return null;
        }

        if (IsOwnProcess(hwnd, ownProcessId))
        {
            return null;
        }

        var className = NativeMethods.GetClassName(hwnd);
        if (ShellClasses.Contains(className))
        {
            return null;
        }

        if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DwmwaCloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
        {
            return null;
        }

        if (!NativeMethods.GetWindowRect(hwnd, out var windowRect) || windowRect.Width < 160 || windowRect.Height < 90)
        {
            return null;
        }

        var frame = windowRect.ToRectangle();
        if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DwmwaExtendedFrameBounds, out NativeMethods.Rect frameRect, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.Rect>()) == 0
            && frameRect.Width > 0 && frameRect.Height > 0)
        {
            frame = frameRect.ToRectangle();
        }

        var processName = ProcessNameOf(hwnd);
        if (processName.Length == 0 || excludedProcesses.Contains(processName))
        {
            return null;
        }

        var (caption, buttons) = CaptionOf(hwnd, windowRect.ToRectangle(), frame);
        if (caption.Width < 48 || caption.Height < 16)
        {
            return null;
        }

        if (IsBorderlessFullscreen(frame))
        {
            return null;
        }

        return new WindowSample(hwnd, processName, className, frame, caption, buttons);
    }

    public static string ProbeForeground()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        var sample = Describe(hwnd, ownProcessId: -1, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (sample is null)
        {
            return $"foreground 0x{hwnd:X} has no usable caption";
        }

        var buttons = sample.Value.Buttons?.ToString() ?? "none";
        return $"{sample.Value.ProcessName} class={sample.Value.ClassName} frame={sample.Value.Frame} caption={sample.Value.Caption} buttons={buttons}";
    }

    private static (Rectangle Caption, Rectangle? Buttons) CaptionOf(IntPtr hwnd, Rectangle window, Rectangle frame)
    {
        Rectangle? buttons = null;
        if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DwmwaCaptionButtonBounds, out NativeMethods.Rect relative, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.Rect>()) == 0
            && relative.Width > 0 && relative.Height > 0)
        {
            buttons = new Rectangle(window.Left + relative.Left, window.Top + relative.Top, relative.Width, relative.Height);
        }

        if (buttons is Rectangle buttonRect)
        {
            var top = Math.Max(frame.Top, buttonRect.Top);
            var bottom = Math.Max(top + 16, buttonRect.Bottom);
            return (Rectangle.FromLTRB(frame.Left, top, frame.Right, bottom), buttonRect);
        }

        int height = Math.Clamp(SystemInformation.CaptionHeight, 28, 40);
        var fallback = new Rectangle(frame.Left, frame.Top, frame.Width, height);
        var assumedButtons = new Rectangle(fallback.Right - 140, fallback.Top, 140, fallback.Height);
        return (fallback, assumedButtons);
    }

    private static bool IsBorderlessFullscreen(Rectangle frame)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var bounds = screen.Bounds;
            if (frame.Width >= bounds.Width - 2 && frame.Height >= bounds.Height - 2
                && Math.Abs(frame.Left - bounds.Left) <= 2 && Math.Abs(frame.Top - bounds.Top) <= 2)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOwnProcess(IntPtr hwnd, int ownProcessId)
    {
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
        return processId == (uint)ownProcessId;
    }

    private static string ProcessNameOf(IntPtr hwnd)
    {
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
        try
        {
            return Process.GetProcessById((int)processId).ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return string.Empty;
        }
    }
}
