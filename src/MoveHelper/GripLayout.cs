namespace MoveHelper;

public enum GripMode
{
    NearPointer,
    Center,
    FullWidth,
}

public enum HitKind
{
    Caption,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

public readonly record struct GripTarget(Rectangle Bounds, HitKind Kind);

/// <summary>
/// Decides where the hover handle sits. Pure geometry so the tray app and the tests share one answer.
/// </summary>
public static class GripLayout
{
    public const int PillWidth = 148;
    public const int PillHeight = 26;
    public const int CornerGrip = 36;
    public const int EdgeMargin = 6;

    public static GripTarget? Find(
        Rectangle frame,
        Rectangle caption,
        Rectangle? captionButtons,
        Point cursor,
        GripMode mode,
        bool cornersEnabled,
        int cornerReach)
    {
        if (cornersEnabled && cornerReach > 0)
        {
            var corner = CornerNear(frame, cursor, cornerReach);
            if (corner is HitKind kind)
            {
                return new GripTarget(SquareOnCorner(frame, kind, CornerGrip), kind);
            }
        }

        if (caption.Width <= 0 || caption.Height <= 0 || !caption.Contains(cursor))
        {
            return null;
        }

        if (captionButtons is Rectangle buttons && Inflate(buttons, 2).Contains(cursor))
        {
            return null;
        }

        var available = AvailableSpan(caption, captionButtons);
        if (available.Width < 48)
        {
            return null;
        }

        int height = Math.Min(PillHeight, Math.Max(18, caption.Height - 4));
        int top = caption.Top + (caption.Height - height) / 2;
        int left = mode switch
        {
            GripMode.FullWidth => available.Left,
            GripMode.Center => available.Left + Math.Max(0, (available.Width - Math.Min(PillWidth, available.Width)) / 2),
            _ => ClampLeft(cursor.X - PillWidth / 2, available.Left, available.Width, Math.Min(PillWidth, available.Width)),
        };
        int width = mode == GripMode.FullWidth
            ? available.Width
            : Math.Min(PillWidth, available.Width);

        return new GripTarget(new Rectangle(left, top, width, height), HitKind.Caption);
    }

    public static int ToHitTest(HitKind kind) => kind switch
    {
        HitKind.TopLeft => 13,
        HitKind.TopRight => 14,
        HitKind.BottomLeft => 16,
        HitKind.BottomRight => 17,
        _ => 2,
    };

    private static Rectangle AvailableSpan(Rectangle caption, Rectangle? captionButtons)
    {
        if (captionButtons is not Rectangle buttons || !caption.IntersectsWith(buttons))
        {
            return InsetX(caption, EdgeMargin);
        }

        bool buttonsOnRight = buttons.Left >= caption.Left + caption.Width / 2;
        if (buttonsOnRight)
        {
            int right = Math.Min(caption.Right - EdgeMargin, buttons.Left - EdgeMargin);
            return Rectangle.FromLTRB(caption.Left + EdgeMargin, caption.Top, Math.Max(caption.Left + EdgeMargin, right), caption.Bottom);
        }

        int left = Math.Max(caption.Left + EdgeMargin, buttons.Right + EdgeMargin);
        return Rectangle.FromLTRB(left, caption.Top, caption.Right - EdgeMargin, caption.Bottom);
    }

    private static Rectangle InsetX(Rectangle rect, int margin) =>
        Rectangle.FromLTRB(rect.Left + margin, rect.Top, Math.Max(rect.Left + margin, rect.Right - margin), rect.Bottom);

    private static int ClampLeft(int desired, int areaLeft, int areaWidth, int gripWidth)
    {
        int maxLeft = areaLeft + Math.Max(0, areaWidth - gripWidth);
        return Math.Clamp(desired, areaLeft, maxLeft);
    }

    private static HitKind? CornerNear(Rectangle frame, Point cursor, int reach)
    {
        (HitKind Kind, Point Point)[] corners =
        [
            (HitKind.TopLeft, new Point(frame.Left, frame.Top)),
            (HitKind.TopRight, new Point(frame.Right, frame.Top)),
            (HitKind.BottomLeft, new Point(frame.Left, frame.Bottom)),
            (HitKind.BottomRight, new Point(frame.Right, frame.Bottom)),
        ];

        HitKind? best = null;
        var bestDistance = long.MaxValue;
        foreach (var (kind, point) in corners)
        {
            long dx = cursor.X - point.X;
            long dy = cursor.Y - point.Y;
            long distance = dx * dx + dy * dy;
            if (distance <= (long)reach * reach && distance < bestDistance)
            {
                best = kind;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static Rectangle SquareOnCorner(Rectangle frame, HitKind kind, int size)
    {
        int half = size / 2;
        int x = kind is HitKind.TopLeft or HitKind.BottomLeft ? frame.Left - half : frame.Right - half;
        int y = kind is HitKind.TopLeft or HitKind.TopRight ? frame.Top - half : frame.Bottom - half;
        return new Rectangle(x, y, size, size);
    }

    private static Rectangle Inflate(Rectangle rect, int amount)
    {
        rect.Inflate(amount, amount);
        return rect;
    }
}
