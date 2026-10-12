using System.Drawing.Drawing2D;

namespace MoveHelper;

internal sealed class GripOverlay : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    private IntPtr _target;
    private HitKind _kind = HitKind.Caption;
    private Rectangle _shown;

    public GripOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AllowTransparency = true;
        Opacity = 0.92;
        BackColor = Color.FromArgb(24, 24, 24);
        AutoScaleMode = AutoScaleMode.None;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public bool IsDragging { get; private set; }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExNoActivate | WsExToolWindow;
            return parameters;
        }
    }

    public void ShowGrip(IntPtr target, GripTarget grip)
    {
        _target = target;
        _kind = grip.Kind;
        Cursor = grip.Kind switch
        {
            HitKind.TopLeft or HitKind.BottomRight => Cursors.SizeNWSE,
            HitKind.TopRight or HitKind.BottomLeft => Cursors.SizeNESW,
            _ => Cursors.SizeAll,
        };

        if (!IsHandleCreated)
        {
            CreateHandle();
        }

        if (_shown != grip.Bounds)
        {
            _shown = grip.Bounds;
            _ = NativeMethods.SetWindowPos(
                Handle,
                NativeMethods.HwndTopMost,
                grip.Bounds.X,
                grip.Bounds.Y,
                grip.Bounds.Width,
                grip.Bounds.Height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
            UpdateRegion();
        }
        else if (!Visible)
        {
            _ = NativeMethods.SetWindowPos(
                Handle,
                NativeMethods.HwndTopMost,
                grip.Bounds.X,
                grip.Bounds.Y,
                grip.Bounds.Width,
                grip.Bounds.Height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }

        Invalidate();
    }

    public void HideGrip()
    {
        _target = IntPtr.Zero;
        _shown = Rectangle.Empty;
        if (Visible)
        {
            Hide();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || _target == IntPtr.Zero || !NativeMethods.IsWindow(_target))
        {
            return;
        }

        var target = _target;
        var hit = GripLayout.ToHitTest(_kind);
        IsDragging = true;
        HideGrip();
        _ = NativeMethods.ReleaseCapture();
        _ = NativeMethods.SendMessage(target, NativeMethods.WmNcLButtonDown, hit, IntPtr.Zero);
        IsDragging = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Rounded(bounds, _kind == HitKind.Caption ? 10 : 6);
        using var fill = new SolidBrush(Color.FromArgb(235, 28, 28, 28));
        using var pen = new Pen(Color.FromArgb(230, 120, 186, 255), 1.6f);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);

        if (_kind == HitKind.Caption)
        {
            DrawMoveMarks(e.Graphics);
        }
        else
        {
            DrawCornerMark(e.Graphics);
        }
    }

    private void UpdateRegion()
    {
        using var path = Rounded(new Rectangle(0, 0, Width, Height), _kind == HitKind.Caption ? 10 : 6);
        Region = new Region(path);
    }

    private void DrawMoveMarks(Graphics graphics)
    {
        int centerX = Width / 2;
        int centerY = Height / 2;
        using var brush = new SolidBrush(Color.FromArgb(230, 210, 226, 240));
        for (int row = -1; row <= 1; row++)
        {
            for (int column = -2; column <= 2; column++)
            {
                graphics.FillEllipse(brush, centerX + column * 8 - 2, centerY + row * 6 - 2, 4, 4);
            }
        }
    }

    private void DrawCornerMark(Graphics graphics)
    {
        using var pen = new Pen(Color.FromArgb(235, 210, 226, 240), 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        int inset = 9;
        Point a;
        Point b;
        Point c;
        switch (_kind)
        {
            case HitKind.TopRight:
                a = new Point(Width - inset, inset + 12);
                b = new Point(Width - inset, inset);
                c = new Point(Width - inset - 12, inset);
                break;
            case HitKind.BottomLeft:
                a = new Point(inset, Height - inset - 12);
                b = new Point(inset, Height - inset);
                c = new Point(inset + 12, Height - inset);
                break;
            case HitKind.BottomRight:
                a = new Point(Width - inset, Height - inset - 12);
                b = new Point(Width - inset, Height - inset);
                c = new Point(Width - inset - 12, Height - inset);
                break;
            default:
                a = new Point(inset, inset + 12);
                b = new Point(inset, inset);
                c = new Point(inset + 12, inset);
                break;
        }

        graphics.DrawLines(pen, new[] { a, b, c });
    }

    private static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        if (bounds.Width < diameter || bounds.Height < diameter)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
