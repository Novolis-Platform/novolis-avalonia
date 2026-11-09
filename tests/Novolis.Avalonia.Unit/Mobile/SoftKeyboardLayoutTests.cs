using Avalonia;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Unit.Mobile;

public sealed class SoftKeyboardLayoutTests
{
    [Test]
    public async Task BottomInset_UsesOccludedHeight_WhenKeyboardCoversBottom()
    {
        // Flip8-ish host; keyboard occupies the lower 420 DIPs.
        var occluded = new Rect(0, 840, 540, 420);
        var inset = SoftKeyboardLayout.BottomInset(occluded, hostHeight: 1260);
        await Assert.That(inset).IsEqualTo(420);
    }

    [Test]
    public async Task BottomInset_IsZero_ForEmptyOccludedRect()
    {
        var inset = SoftKeyboardLayout.BottomInset(default, hostHeight: 1260);
        await Assert.That(inset).IsEqualTo(0);
    }

    [Test]
    public async Task BottomInset_Clamps_ToHostHeight()
    {
        var occluded = new Rect(0, 0, 540, 2000);
        var inset = SoftKeyboardLayout.BottomInset(occluded, hostHeight: 800);
        await Assert.That(inset).IsEqualTo(800);
    }

    [Test]
    public async Task WithKeyboardBottom_Squishes_WithoutChangingOtherSides()
    {
        var padded = SoftKeyboardLayout.WithKeyboardBottom(new Thickness(4, 8, 4, 2), 320);
        await Assert.That(padded).IsEqualTo(new Thickness(4, 8, 4, 322));
    }

    [Test]
    public async Task CaretNeedsScroll_WhenCaretBelowVisibleBottom()
    {
        await Assert.That(SoftKeyboardLayout.CaretNeedsScroll(caretBottom: 900, visibleBottom: 700))
            .IsTrue();
        await Assert.That(SoftKeyboardLayout.CaretNeedsScroll(caretBottom: 500, visibleBottom: 700))
            .IsFalse();
    }
}
