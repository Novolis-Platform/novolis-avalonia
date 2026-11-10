using Avalonia;
using Avalonia.Input;

namespace Novolis.Avalonia.Controls.Sketch;

public sealed partial class SketchControl
{
    bool TryEndTextEditOnPress()
    {
        if (_textEditor is null)
            return false;
        EndTextEdit(commit: true);
        InvalidateVisual();
        return true;
    }

    bool TryBeginPan(PointerPressedEventArgs e, Point screen, PointerPointProperties props)
    {
        if (!props.IsMiddleButtonPressed && !(_spaceDown && props.IsLeftButtonPressed))
            return false;
        _panning = true;
        e.Pointer.Capture(this);
        e.Handled = true;
        return true;
    }

    bool TryHandleToolPress(
        PointerPressedEventArgs e,
        SketchPoint world,
        Point screen)
    {
        switch (Tool)
        {
            case SketchTool.Pen:
                _draft = [world];
                _dragMode = DragMode.Draw;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return true;

            case SketchTool.Line:
            case SketchTool.Spline:
                HandleVertexClick(world, e.ClickCount);
                e.Handled = true;
                InvalidateVisual();
                return true;

            case SketchTool.Rect:
            case SketchTool.Ellipse:
            case SketchTool.SpeechBubble:
            case SketchTool.TextBox:
                _draft = [world, world];
                _dragMode = DragMode.ShapeDrag;
                _dragStartWorld = world;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return true;

            case SketchTool.Text:
                PlaceTextLabel(world);
                e.Handled = true;
                return true;

            case SketchTool.Eraser:
                _eraserBatch = [];
                EraseAt(world);
                _dragMode = DragMode.Erase;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return true;

            case SketchTool.Fill:
                ApplyPaintBucket(screen);
                e.Handled = true;
                InvalidateVisual();
                return true;
        }

        return false;
    }

    bool TryHandleSelectionPress(
        PointerPressedEventArgs e,
        SketchDocument doc,
        SketchPoint world,
        Point screen,
        bool shift,
        bool ctrl)
    {
        if (TryHitRotateGrip(screen))
        {
            BeginGeometryDrag(doc, DragMode.Rotate, world);
            e.Pointer.Capture(this);
            e.Handled = true;
            return true;
        }

        if (TryHitGrip(screen, out var grip, out _))
        {
            BeginGeometryDrag(doc, DragMode.Resize, world);
            _activeGrip = grip;
            e.Pointer.Capture(this);
            e.Handled = true;
            return true;
        }

        var hit = HitTestStroke(screen);
        if (hit is null)
            return false;

        if (e.ClickCount >= 2 && hit.Kind is SketchElementKind.Text or SketchElementKind.TextBox)
        {
            doc.Select(hit.Id);
            RaiseSelectionIfNeeded();
            BeginTextEdit(hit);
            e.Handled = true;
            InvalidateVisual();
            return true;
        }

        if (shift || ctrl)
            doc.ToggleSelection(hit.Id);
        else if (!doc.Selection.Contains(hit.Id))
            doc.Select(hit.Id);

        RaiseSelectionIfNeeded();
        if (doc.Selection.Contains(hit.Id))
            BeginGeometryDrag(doc, DragMode.Move, world);

        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
        return true;
    }

    void BeginMarquee(PointerPressedEventArgs e, SketchDocument doc, Point screen, bool shift, bool ctrl)
    {
        _dragMode = DragMode.Marquee;
        _marqueeStartScreen = screen;
        _marqueeScreen = new Rect(screen, screen);
        _marqueeAdditive = shift || ctrl;
        if (!_marqueeAdditive)
            doc.Select(null);
        RaiseSelectionIfNeeded();
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }
}
