using NAudio.Wave;

namespace Novolis.Avalonia.Audio;

/// <summary>
/// Converts 16-bit PCM <see cref="IWaveProvider"/> to IEEE float samples.
/// (NAudio's <c>WaveToSampleProvider</c> only accepts float sources.)
/// </summary>
public sealed class Pcm16BitToSampleProvider : ISampleProvider
{
    readonly IWaveProvider _source;
    readonly byte[] _buffer = new byte[4096];

    public Pcm16BitToSampleProvider(IWaveProvider source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.WaveFormat.Encoding != WaveFormatEncoding.Pcm || source.WaveFormat.BitsPerSample != 16)
            throw new ArgumentException("Expected 16-bit PCM.", nameof(source));
        _source = source;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, source.WaveFormat.Channels);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(Span<float> buffer)
    {
        var totalSamples = 0;
        while (totalSamples < buffer.Length)
        {
            var toRead = Math.Min(_buffer.Length, (buffer.Length - totalSamples) * 2);
            var read = _source.Read(_buffer.AsSpan(0, toRead));
            if (read <= 0)
                break;
            if ((read & 1) != 0)
                read--;
            for (var i = 0; i < read; i += 2)
            {
                var sample = (short)(_buffer[i] | (_buffer[i + 1] << 8));
                buffer[totalSamples] = sample / 32768f;
                totalSamples++;
            }
        }

        return totalSamples;
    }
}
