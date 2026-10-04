using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Novolis.Audio.Core;

namespace Novolis.Avalonia.Audio;

/// <summary>Overlapping short PCM previews for polyphonic piano keys.</summary>
public sealed class MidiPreviewMixer : IDisposable
{
    readonly object _gate = new();
    readonly List<OneShot> _active = [];
#pragma warning disable CS0618
    WasapiOut? _waveOut;
#pragma warning restore CS0618
    MixingSampleProvider? _mixer;
    bool _disposed;

    /// <summary>Plays <paramref name="pcm"/> mixed with any currently sounding notes.</summary>
    public void Play(PcmBuffer pcm)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        if (pcm.Format.SampleFormat != PcmSampleFormat.Int16)
            throw new NotSupportedException("Preview supports Int16 only.");

        lock (_gate)
        {
            EnsureEngine(pcm.Format);
            PruneFinished();
            var stream = new RawSourceWaveStream(
                new MemoryStream(pcm.Samples.ToArray()),
                WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.Pcm,
                    pcm.Format.SampleRate,
                    pcm.Format.Channels,
                    pcm.Format.SampleRate * pcm.Format.BytesPerFrame,
                    pcm.Format.BytesPerFrame,
                    16));
            var shot = new OneShot(new Pcm16BitToSampleProvider(stream));
            _active.Add(shot);
            _mixer!.AddMixerInput(shot);
            if (_waveOut!.PlaybackState != PlaybackState.Playing)
                _waveOut.Play();
        }
    }

    /// <summary>Stops all voices.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            _waveOut?.Stop();
            if (_mixer is not null)
            {
                foreach (var shot in _active)
                    _mixer.RemoveMixerInput(shot);
            }

            _active.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        lock (_gate)
        {
            Stop();
            _waveOut?.Dispose();
            _waveOut = null;
            _mixer = null;
        }
    }

    void PruneFinished()
    {
        if (_mixer is null)
            return;
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            if (!_active[i].Finished)
                continue;
            _mixer.RemoveMixerInput(_active[i]);
            _active.RemoveAt(i);
        }
    }

    void EnsureEngine(PcmFormat format)
    {
        if (_waveOut is not null && _mixer is not null)
            return;

        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels);
        _mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
#pragma warning disable CS0618
        _waveOut = new WasapiOut();
#pragma warning restore CS0618
        _waveOut.Init(_mixer);
    }

    sealed class OneShot : ISampleProvider
    {
        readonly ISampleProvider _source;

        public OneShot(ISampleProvider source) => _source = source;

        public WaveFormat WaveFormat => _source.WaveFormat;
        public bool Finished { get; private set; }

        public int Read(Span<float> buffer)
        {
            if (Finished)
            {
                buffer.Clear();
                return buffer.Length;
            }

            var read = _source.Read(buffer);
            if (read < buffer.Length)
            {
                buffer[read..].Clear();
                Finished = true;
                return buffer.Length;
            }

            return read;
        }
    }
}
