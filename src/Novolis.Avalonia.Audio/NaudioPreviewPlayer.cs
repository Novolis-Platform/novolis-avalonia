using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Novolis.Audio.Core;

namespace Novolis.Avalonia.Audio;

/// <summary>WaveOut preview for arrangement mixdowns (same Int16→float path as piano preview).</summary>
public sealed class NaudioPreviewPlayer : IDisposable
{
    WaveOutEvent? _waveOut;
    WaveStream? _stream;
    bool _disposed;

    /// <summary>True while WaveOut is actively playing.</summary>
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    /// <summary>Starts playing <paramref name="pcm"/> from the beginning.</summary>
    public void Play(PcmBuffer pcm)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        if (pcm.Format.SampleFormat != PcmSampleFormat.Int16)
            throw new NotSupportedException("Preview supports Int16 only.");
        if (pcm.FrameCount <= 0 || pcm.Samples.Length == 0)
            throw new InvalidOperationException("Nothing to play — mix is empty.");

        Stop();
        var format = WaveFormat.CreateCustomFormat(
            WaveFormatEncoding.Pcm,
            pcm.Format.SampleRate,
            pcm.Format.Channels,
            pcm.Format.SampleRate * pcm.Format.BytesPerFrame,
            pcm.Format.BytesPerFrame,
            16);
        _stream = new RawSourceWaveStream(new MemoryStream(pcm.Samples.ToArray()), format);
        var samples = new Pcm16BitToSampleProvider(_stream);
        _waveOut = new WaveOutEvent { DesiredLatency = 80 };
        _waveOut.Init(samples);
        _waveOut.Play();
    }

    /// <summary>Stops playback.</summary>
    public void Stop()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _stream?.Dispose();
        _stream = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
    }
}
