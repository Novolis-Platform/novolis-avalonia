using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Controls;

/// <summary>
/// Renders a compact, protocol-neutral piece availability bitmap.
/// </summary>
public sealed class PeerPieceMap : Control
{
    private IReadOnlyList<bool> _pieces = [];

    /// <summary>Creates an empty piece map.</summary>
    public PeerPieceMap()
    {
        MinWidth = 160;
        Width = 180;
        MinHeight = 22;
        Height = 22;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        IsHitTestVisible = false;
    }

    /// <summary>Piece availability values, in content order.</summary>
    public IReadOnlyList<bool> Pieces => _pieces;

    /// <summary>Replaces the availability bitmap and repaints the control.</summary>
    public void SetPieces(IEnumerable<bool>? pieces)
    {
        _pieces = pieces?.ToArray() ?? [];
        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        var outline = bounds.Deflate(1);
        context.DrawRectangle(
            Profile.RaisedBrush,
            new Pen(Profile.BorderBrush, GraphicalProfileColors.Stroke),
            outline);

        if (_pieces.Count == 0)
        {
            return;
        }

        const int maximumCells = 256;
        var cellCount = Math.Min(_pieces.Count, maximumCells);
        var gap = cellCount > 128 ? 0d : 1d;
        var cellWidth = Math.Max(1d, (outline.Width - gap * (cellCount - 1)) / cellCount);
        for (var index = 0; index < cellCount; index++)
        {
            var x = outline.X + index * (cellWidth + gap);
            var cell = new Rect(x, outline.Y, cellWidth, outline.Height);
            context.DrawRectangle(
                _pieces[index]
                    ? Profile.AccentFillBrush
                    : Profile.BackgroundBrush,
                null,
                cell);
        }
    }
}
