using Avalonia.Controls;
using Avalonia.Layout;
using global::Avalonia.Media;
using Novolis.Audio.Live.Protocol.Dto;

namespace Novolis.Avalonia.Live;

public sealed class LiveTransportStatusPanel : StackPanel
{
    private readonly TextBlock _program = new();
    private readonly TextBlock _timing = new();
    private readonly TextBlock _swap = new();
    private readonly TextBlock _error = new();

    public LiveTransportStatusPanel()
    {
        Spacing = 6;
        Orientation = Orientation.Vertical;

        Children.Add(new TextBlock { Text = "Live transport", FontWeight = FontWeight.Bold });
        Children.Add(_program);
        Children.Add(_timing);
        Children.Add(_swap);
        Children.Add(_error);
    }

    public void Bind(LiveTransportSnapshotDto? snapshot)
    {
        if (snapshot is null)
        {
            _program.Text = "No active program.";
            _timing.Text = string.Empty;
            _swap.Text = string.Empty;
            _error.Text = string.Empty;
            return;
        }

        _program.Text = snapshot.ActiveProgramId is null
            ? "No active program."
            : $"Program {snapshot.ActiveProgramId} v{snapshot.ActiveVersion} @ {snapshot.Bpm:0.###} BPM";
        _timing.Text = $"Beat {snapshot.Beat:0.###} | Bar {snapshot.Bar} | Phrase {snapshot.Phrase}";
        _swap.Text = snapshot.PendingProgramId is null
            ? "No queued swap."
            : $"Queued {snapshot.PendingProgramId} via {snapshot.PendingSwapPolicy}";
        _error.Text = string.IsNullOrWhiteSpace(snapshot.LastError) ? string.Empty : snapshot.LastError!;
    }
}
