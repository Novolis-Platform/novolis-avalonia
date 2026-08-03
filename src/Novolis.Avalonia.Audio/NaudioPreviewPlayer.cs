using NAudio.Wave;
using Novolis.Audio.Core;

namespace Novolis.Avalonia.Audio;

/// <summary>Lightweight WaveOut preview for arrangement mixdowns.</summary>
public sealed class NaudioPreviewPlayer : IDisposable
{
    WaveOutEvent? _waveOut;
    BufferedWaveProvider? _provider;
    bool _disposed;

    /// <summary>Starts playing <paramref name="pcm"/> from the beginning.</summary>
    public void Play(PcmBuffer pcm)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        if (pcm.Format.SampleFormat != PcmSampleFormat.Int16)
            throw new NotSupportedException("Preview supports Int16 only.");

        Stop();
        var format = WaveFormat.CreateCustomFormat(
            WaveFormatEncoding.Pcm,
            pcm.Format.SampleRate,
            pcm.Format.Channels,
            pcm.Format.SampleRate * pcm.Format.BytesPerFrame,
            pcm.Format.BytesPerFrame,
            16);
        _provider = new BufferedWaveProvider(format)
        {
            BufferDuration = TimeSpan.FromSeconds(Math.Max(1, pcm.Duration.TotalSeconds + 0.5)),
            DiscardOnBufferOverflow = true,
        };
        _provider.AddSamples(pcm.Samples.ToArray(), 0, pcm.Samples.Length);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_provider);
        _waveOut.Play();
    }

    /// <summary>Stops playback.</summary>
    public void Stop()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _provider = null;
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
