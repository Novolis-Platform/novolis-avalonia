using Avalonia;
using Avalonia.Input;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

public sealed partial class ArrangementTimelineControl
{
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_project is null)
            return;

        var pos = e.GetPosition(this);
        e.Pointer.Capture(this);

        if (pos.Y < RulerHeight)
        {
            HandleRulerPress(pos);
            return;
        }

        var trackIndex = TrackIndexAt(pos.Y);
        if (trackIndex < 0 || trackIndex >= _project.Tracks.Count)
            return;

        var track = _project.Tracks[trackIndex];
        if (pos.X < HeaderWidth)
        {
            HandleTrackHeaderPress(track, trackIndex, pos);
            return;
        }

        HandleTimelinePress(track, pos);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_project is null || _gesture is null)
            return;

        var pos = e.GetPosition(this);
        var time = TimeFromX(pos.X);
        DispatchGestureDrag(_gesture, pos, time);
        InvalidateVisual();
    }

    void HandleRulerPress(Point pos)
    {
        var time = TimeFromX(pos.X);
        _selectionStart = time;
        _selectionEnd = time;
        SeekRequested?.Invoke(time);
        _gesture = new PointerGesture(GestureKind.SelectRange, null, null, time, pos);
        InvalidateVisual();
    }

    void HandleTrackHeaderPress(ArrangementTrack track, int trackIndex, Point pos)
    {
        var localY = pos.Y - (RulerHeight + trackIndex * TrackHeight);
        if (localY <= MuteSoloHeight)
        {
            BeforeMutation?.Invoke();
            if (pos.X < HeaderWidth * 0.5)
                track.Mute = !track.Mute;
            else
                track.Solo = !track.Solo;
            ArrangementChanged?.Invoke();
        }
        else
        {
            _selectedTrackId = track.Id;
            TrackSelected?.Invoke(track.Id);
        }

        InvalidateVisual();
    }

    void HandleTimelinePress(ArrangementTrack track, Point pos)
    {
        var timeAt = TimeFromX(pos.X);
        SeekRequested?.Invoke(timeAt);
        _selectedTrackId = track.Id;
        TrackSelected?.Invoke(track.Id);

        var hit = FindClipAt(track, timeAt);
        if (TryHandleToolPress(track, timeAt, hit))
            return;

        if (hit is null)
        {
            BeginEmptyTimelineSelect(track, timeAt, pos);
            return;
        }

        SelectClipAndBeginGesture(hit, track, timeAt, pos);
    }

    static ArrangementClip? FindClipAt(ArrangementTrack track, TimeSpan timeAt)
    {
        foreach (var clip in track.Clips)
        {
            if (clip.Contains(timeAt))
                return clip;
        }

        return null;
    }

    bool TryHandleToolPress(ArrangementTrack track, TimeSpan timeAt, ArrangementClip? hit)
    {
        if (Tool is ArrangementTool.Delete && hit is not null)
        {
            DeleteClipRequested?.Invoke(hit.Id);
            return true;
        }

        if (Tool is ArrangementTool.Split)
        {
            if (hit is not null)
            {
                _selectedClipId = hit.Id;
                ClipSelected?.Invoke(hit.Id);
                SplitAtRequested?.Invoke(timeAt);
            }

            return true;
        }

        if (Tool is ArrangementTool.Draw && hit is null)
        {
            DrawAtRequested?.Invoke(track.Id, timeAt);
            return true;
        }

        return false;
    }

    void BeginEmptyTimelineSelect(ArrangementTrack track, TimeSpan timeAt, Point pos)
    {
        if (Tool is ArrangementTool.Select)
        {
            _gesture = new PointerGesture(GestureKind.SelectRange, null, track.Id, timeAt, pos);
            _selectionStart = timeAt;
            _selectionEnd = timeAt;
        }

        InvalidateVisual();
    }

    void SelectClipAndBeginGesture(ArrangementClip hit, ArrangementTrack track, TimeSpan timeAt, Point pos)
    {
        _selectedClipId = hit.Id;
        ClipSelected?.Invoke(hit.Id);

        if (Tool is not (ArrangementTool.Select or ArrangementTool.Move))
        {
            InvalidateVisual();
            return;
        }

        var kind = GestureKindForClipEdge(hit, pos);
        BeforeMutation?.Invoke();
        _gesture = new PointerGesture(kind, hit.Id, track.Id, timeAt, pos)
        {
            OriginStart = hit.TimelineStart,
            OriginDuration = hit.Duration,
            OriginOffset = hit.SourceOffset,
            OriginTrackId = track.Id,
        };
        InvalidateVisual();
    }

    GestureKind GestureKindForClipEdge(ArrangementClip hit, Point pos)
    {
        var clipLeft = HeaderWidth + hit.TimelineStart.TotalSeconds * PixelsPerSecond;
        var clipRight = HeaderWidth + hit.TimelineEnd.TotalSeconds * PixelsPerSecond;
        if (Tool is ArrangementTool.Select && pos.X - clipLeft < 8)
            return GestureKind.TrimStart;
        if (Tool is ArrangementTool.Select && clipRight - pos.X < 8)
            return GestureKind.TrimEnd;
        return GestureKind.MoveClip;
    }

    void DispatchGestureDrag(PointerGesture gesture, Point pos, TimeSpan time)
    {
        switch (gesture.Kind)
        {
            case GestureKind.SelectRange:
                DragSelectRange(time);
                break;
            case GestureKind.MoveClip:
                DragMoveClip(gesture, pos, time);
                break;
            case GestureKind.TrimStart:
                DragTrimStart(gesture, time);
                break;
            case GestureKind.TrimEnd:
                DragTrimEnd(gesture, time);
                break;
        }
    }

    void DragSelectRange(TimeSpan time)
    {
        _selectionEnd = time;
        SeekRequested?.Invoke(time);
    }

    void DragMoveClip(PointerGesture gesture, Point pos, TimeSpan time)
    {
        if (gesture.ClipId is not { } moveId)
            return;

        var clip = _project!.FindClip(moveId);
        if (clip is null || gesture.OriginStart is not { } origin)
            return;

        var delta = time - gesture.AnchorTime;
        var next = origin + delta;
        if (next < TimeSpan.Zero)
            next = TimeSpan.Zero;
        clip.TimelineStart = TimeSpan.FromSeconds(Math.Round(next.TotalSeconds * 20) / 20.0);

        TryMoveClipToTrackUnderPointer(gesture, moveId, pos);
    }

    void TryMoveClipToTrackUnderPointer(PointerGesture gesture, Guid moveId, Point pos)
    {
        var destIndex = TrackIndexAt(pos.Y);
        if (destIndex < 0 || destIndex >= _project!.Tracks.Count)
            return;

        var dest = _project.Tracks[destIndex];
        if (gesture.OriginTrackId is not { } from || from == dest.Id)
            return;

        AudioEditOps.MoveClipToTrack(_project, moveId, dest.Id);
        gesture.OriginTrackId = dest.Id;
        _selectedTrackId = dest.Id;
        TrackSelected?.Invoke(dest.Id);
    }

    void DragTrimStart(PointerGesture gesture, TimeSpan time)
    {
        if (gesture.ClipId is not { } clipId
            || gesture.OriginStart is not { } oStart
            || gesture.OriginDuration is not { } oDur
            || gesture.OriginOffset is not { } oOff)
            return;

        var clip = _project!.FindClip(clipId);
        if (clip is null)
            return;

        var clamped = time < oStart ? oStart : time;
        if (clamped >= oStart + oDur - TimeSpan.FromMilliseconds(30))
            return;

        var d = clamped - oStart;
        clip.TimelineStart = clamped;
        clip.SourceOffset = oOff + d;
        clip.Duration = oDur - d;
    }

    void DragTrimEnd(PointerGesture gesture, TimeSpan time)
    {
        if (gesture.ClipId is not { } clipId
            || gesture.OriginStart is not { } oStart
            || gesture.OriginDuration is not { } oDur
            || gesture.OriginOffset is not { } oOff)
            return;

        var clip = _project!.FindClip(clipId);
        if (clip is null)
            return;

        var asset = _project.FindAsset(clip.AssetId);
        var maxDur = asset is null ? oDur : asset.Duration - oOff;
        var newEnd = time < oStart + TimeSpan.FromMilliseconds(30)
            ? oStart + TimeSpan.FromMilliseconds(30)
            : time;
        var newDur = newEnd - oStart;
        if (newDur > maxDur)
            newDur = maxDur;
        clip.TimelineStart = oStart;
        clip.SourceOffset = oOff;
        clip.Duration = newDur;
    }
}
