namespace MoveHelper;

internal sealed class TrayApp : ApplicationContext
{
    private readonly int _processId = Environment.ProcessId;
    private readonly NotifyIcon _tray;
    private readonly Bitmap _iconBitmap = new(32, 32);
    private readonly GripOverlay _grip = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 32 };
    private readonly Settings _settings;
    private readonly HashSet<string> _excluded;

    private IntPtr _hoverHwnd;
    private HitKind _hoverKind;
    private long _hoverStarted;
    private IntPtr _lockedHwnd;

    public TrayApp()
    {
        _settings = SettingsStore.Load();
        _excluded = new HashSet<string>(_settings.ExcludedProcesses, StringComparer.OrdinalIgnoreCase);
        _tray = new NotifyIcon
        {
            Icon = CreateIcon(),
            Visible = true,
            Text = "MoveHelper",
        };
        var menu = new ContextMenuStrip();
        menu.Opening += (_, _) => Populate(menu);
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => SetEnabled(!_settings.Enabled);
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        UpdateText();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _grip.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _iconBitmap.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Tick()
    {
        if (_grip.IsDragging || !_settings.Enabled)
        {
            return;
        }

        var hwnd = WindowFinder.RootUnderCursor(_processId);
        if (hwnd == IntPtr.Zero && _lockedHwnd != IntPtr.Zero && NativeMethods.IsWindow(_lockedHwnd))
        {
            hwnd = _lockedHwnd;
        }

        var sample = WindowFinder.Describe(hwnd, _processId, _excluded);
        if (sample is null)
        {
            ResetHover();
            _grip.HideGrip();
            return;
        }

        if (!NativeMethods.GetCursorPos(out var point))
        {
            return;
        }

        var cursor = new Point(point.X, point.Y);
        var framePad = Rectangle.Inflate(sample.Value.Frame, 24, 24);
        if (hwnd == _lockedHwnd && !framePad.Contains(cursor) && !_grip.Bounds.Contains(cursor))
        {
            ResetHover();
            _grip.HideGrip();
            return;
        }

        var target = GripLayout.Find(
            sample.Value.Frame,
            sample.Value.Caption,
            sample.Value.Buttons,
            cursor,
            _settings.Mode,
            _settings.CornersEnabled,
            _settings.CornerReach);

        if (target is null)
        {
            ResetHover();
            _grip.HideGrip();
            return;
        }

        var now = Environment.TickCount64;
        if (sample.Value.Hwnd != _hoverHwnd || target.Value.Kind != _hoverKind)
        {
            _hoverHwnd = sample.Value.Hwnd;
            _hoverKind = target.Value.Kind;
            _hoverStarted = now;
            _lockedHwnd = IntPtr.Zero;
            _grip.HideGrip();
            return;
        }

        if (now - _hoverStarted < _settings.HoverDelayMs)
        {
            return;
        }

        _lockedHwnd = sample.Value.Hwnd;
        _grip.ShowGrip(sample.Value.Hwnd, target.Value);
    }

    private void ResetHover()
    {
        _hoverHwnd = IntPtr.Zero;
        _lockedHwnd = IntPtr.Zero;
        _hoverStarted = 0;
    }

    private void SetEnabled(bool enabled)
    {
        _settings.Enabled = enabled;
        if (!enabled)
        {
            ResetHover();
            _grip.HideGrip();
        }

        Persist();
        UpdateText();
    }

    private void SetMode(GripMode mode)
    {
        _settings.Mode = mode;
        _grip.HideGrip();
        ResetHover();
        Persist();
    }

    private void SetDelay(int milliseconds)
    {
        _settings.HoverDelayMs = milliseconds;
        Persist();
    }

    private void SetCorners(bool enabled)
    {
        _settings.CornersEnabled = enabled;
        _grip.HideGrip();
        ResetHover();
        Persist();
    }

    private void Persist()
    {
        _settings.ExcludedProcesses = _excluded.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        SettingsStore.Save(_settings);
    }

    private void UpdateText()
    {
        _tray.Text = _settings.Enabled ? "MoveHelper — hover a title bar" : "MoveHelper — paused";
    }

    private void Populate(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        menu.Items.Add(Check("Enabled", _settings.Enabled, () => SetEnabled(!_settings.Enabled)));

        var modes = new ToolStripMenuItem("Handle");
        modes.DropDownItems.Add(Check("Near the pointer", _settings.Mode == GripMode.NearPointer, () => SetMode(GripMode.NearPointer)));
        modes.DropDownItems.Add(Check("Center of the title bar", _settings.Mode == GripMode.Center, () => SetMode(GripMode.Center)));
        modes.DropDownItems.Add(Check("Across the title bar", _settings.Mode == GripMode.FullWidth, () => SetMode(GripMode.FullWidth)));
        menu.Items.Add(modes);

        var delays = new ToolStripMenuItem("Hover delay");
        foreach (var milliseconds in new[] { 100, 200, 400 })
        {
            var value = milliseconds;
            delays.DropDownItems.Add(Check($"{value} ms", _settings.HoverDelayMs == value, () => SetDelay(value)));
        }

        menu.Items.Add(delays);
        menu.Items.Add(Check("Larger corners", _settings.CornersEnabled, () => SetCorners(!_settings.CornersEnabled)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
    }

    private static ToolStripMenuItem Check(string text, bool check, Action action)
    {
        var item = new ToolStripMenuItem(text) { Checked = check };
        item.Click += (_, _) => action();
        return item;
    }

    private Icon CreateIcon()
    {
        using (var graphics = Graphics.FromImage(_iconBitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(255, 32, 36, 42));
            using var pen = new Pen(Color.FromArgb(255, 120, 186, 255), 2f);
            graphics.FillEllipse(brush, 2, 2, 28, 28);
            graphics.DrawEllipse(pen, 3, 3, 26, 26);
            using var mark = new SolidBrush(Color.FromArgb(255, 230, 236, 242));
            graphics.FillRectangle(mark, 8, 14, 16, 3);
            graphics.FillPolygon(mark, new[] { new Point(16, 8), new Point(12, 13), new Point(20, 13) });
            graphics.FillPolygon(mark, new[] { new Point(16, 24), new Point(12, 19), new Point(20, 19) });
        }

        return Icon.FromHandle(_iconBitmap.GetHicon());
    }
}
