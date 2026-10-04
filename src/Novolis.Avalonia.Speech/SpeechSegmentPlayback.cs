using System.Security.Cryptography;
using System.Text;
using Novolis.Audio.Voice.AzureSpeech;
using Novolis.Manuscript.Export.Audio;

namespace Novolis.Avalonia.Speech;

/// <summary>Caches Azure MP3 segments and plays a planned speech sequence.</summary>
public sealed class SpeechSegmentPlayback
{
    private readonly SpeechFront front;
    private readonly string cacheDirectory;

    /// <summary>Creates a playback helper over the shared speech front and a cache folder.</summary>
    public SpeechSegmentPlayback(SpeechFront front, string cacheDirectory)
    {
        this.front = front ?? throw new ArgumentNullException(nameof(front));
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectory);
        this.cacheDirectory = cacheDirectory;
        Directory.CreateDirectory(cacheDirectory);
    }

    /// <summary>Returns cached audio or synthesizes and stores an MP3 for one segment.</summary>
    public async Task<byte[]> GetOrSynthesizeAsync(
        string text,
        VoiceSettings voice,
        AzureSpeechSynthesisOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(voice);
        ArgumentNullException.ThrowIfNull(options);
        var path = CachePath(text, voice);
        if (File.Exists(path))
        {
            var cached = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            if (cached.Length > 0)
                return cached;
        }

        var mp3 = await front.CreateMp3Async(text, options, cancellationToken).ConfigureAwait(false);
        try
        {
            await File.WriteAllBytesAsync(path, mp3, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Cache writes are best-effort; playback still succeeds.
        }

        return mp3;
    }

    /// <summary>Returns whether every spoken segment is already cached.</summary>
    public bool HasCachedAudio(string text, VoiceSettings voice, bool speakTitle)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(voice);
        if (string.IsNullOrWhiteSpace(text) || !front.IsAzureConfigured)
            return false;

        var plan = SpeechPlanner.Create(text, voice.ToSpeechOptions(), speakTitle);
        var any = false;
        foreach (var segment in plan.Segments)
        {
            if (segment.Kind != SpeechSegmentKind.Text || string.IsNullOrWhiteSpace(segment.Text))
                continue;
            any = true;
            if (!File.Exists(CachePath(segment.Text, voice)))
                return false;
        }

        return any;
    }

    /// <summary>Concatenates Azure MP3 segments for a planned document.</summary>
    public async Task<byte[]> ConcatenateMp3Async(
        string text,
        VoiceSettings voice,
        AzureSpeechSynthesisOptions options,
        bool speakTitle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(voice);
        ArgumentNullException.ThrowIfNull(options);
        var plan = SpeechPlanner.Create(text, voice.ToSpeechOptions(), speakTitle);
        using var output = new MemoryStream();
        foreach (var segment in plan.Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (segment.Kind != SpeechSegmentKind.Text || string.IsNullOrWhiteSpace(segment.Text))
                continue;
            var mp3 = await GetOrSynthesizeAsync(segment.Text, voice, options, cancellationToken)
                .ConfigureAwait(false);
            await output.WriteAsync(mp3, cancellationToken).ConfigureAwait(false);
        }

        return output.ToArray();
    }

    /// <summary>Stable cache path for one spoken segment and voice settings.</summary>
    public string CachePath(string text, VoiceSettings voice)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(voice);
        var cacheKey = string.Join(
            "\n",
            voice.Voice,
            voice.RatePercent,
            voice.PitchHertz,
            voice.VolumePercent,
            text);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey)))
            .ToLowerInvariant();
        return Path.Combine(cacheDirectory, hash + ".mp3");
    }
}
