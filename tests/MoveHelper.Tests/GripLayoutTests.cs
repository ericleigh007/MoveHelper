using Xunit;

namespace MoveHelper.Tests;

public class GripLayoutTests
{
    private static readonly Rectangle Frame = new(0, 0, 800, 600);
    private static readonly Rectangle Caption = new(0, 0, 800, 32);
    private static readonly Rectangle Buttons = new(680, 0, 120, 32);

    [Fact]
    public void NearPointer_stays_left_of_the_caption_buttons()
    {
        var grip = GripLayout.Find(Frame, Caption, Buttons, new Point(100, 16), GripMode.NearPointer, cornersEnabled: false, cornerReach: 28);

        Assert.NotNull(grip);
        Assert.Equal(HitKind.Caption, grip.Value.Kind);
        Assert.True(grip.Value.Bounds.Right <= Buttons.Left - GripLayout.EdgeMargin);
        Assert.InRange(100, grip.Value.Bounds.Left, grip.Value.Bounds.Right);
    }

    [Fact]
    public void NearPointer_clamps_before_the_buttons()
    {
        var grip = GripLayout.Find(Frame, Caption, Buttons, new Point(650, 16), GripMode.NearPointer, cornersEnabled: false, cornerReach: 28);

        Assert.NotNull(grip);
        Assert.True(grip.Value.Bounds.Right <= Buttons.Left - GripLayout.EdgeMargin);
        Assert.Equal(GripLayout.PillWidth, grip.Value.Bounds.Width);
    }

    [Fact]
    public void Center_sits_in_the_open_part_of_the_caption()
    {
        var grip = GripLayout.Find(Frame, Caption, Buttons, new Point(200, 10), GripMode.Center, cornersEnabled: false, cornerReach: 28);

        Assert.NotNull(grip);
        int availableRight = Buttons.Left - GripLayout.EdgeMargin;
        int availableLeft = GripLayout.EdgeMargin;
        int expected = availableLeft + (availableRight - availableLeft - GripLayout.PillWidth) / 2;
        Assert.Equal(expected, grip.Value.Bounds.Left);
    }

    [Fact]
    public void FullWidth_fills_the_open_caption()
    {
        var grip = GripLayout.Find(Frame, Caption, Buttons, new Point(40, 10), GripMode.FullWidth, cornersEnabled: false, cornerReach: 28);

        Assert.NotNull(grip);
        Assert.Equal(GripLayout.EdgeMargin, grip.Value.Bounds.Left);
        Assert.Equal(Buttons.Left - GripLayout.EdgeMargin * 2, grip.Value.Bounds.Width);
    }

    [Fact]
    public void Cursor_on_the_buttons_does_not_grow_a_move_handle()
    {
        var grip = GripLayout.Find(Frame, Caption, Buttons, new Point(740, 16), GripMode.NearPointer, cornersEnabled: true, cornerReach: 28);

        Assert.Null(grip);
    }

    [Fact]
    public void Corner_wins_when_the_pointer_is_on_the_corner()
    {
        var grip = GripLayout.Find(Frame, Caption, Buttons, new Point(4, 4), GripMode.NearPointer, cornersEnabled: true, cornerReach: 28);

        Assert.NotNull(grip);
        Assert.Equal(HitKind.TopLeft, grip.Value.Kind);
    }

    [Fact]
    public void Content_area_has_no_handle()
    {
        var grip = GripLayout.Find(Frame, Caption, Buttons, new Point(400, 300), GripMode.FullWidth, cornersEnabled: true, cornerReach: 28);

        Assert.Null(grip);
    }
}
