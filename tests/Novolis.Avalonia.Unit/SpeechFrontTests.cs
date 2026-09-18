using Novolis.Audio.Voice;
using Novolis.Avalonia.Mobile;
using Novolis.Avalonia.Speech;

namespace Novolis.Avalonia.Unit;

public sealed class SpeechFrontTests
{
    [Test]
    public async Task Default_front_reads_with_device_voice()
    {
        var voice = new CapturingVoice();
        var store = new MemoryTokenStore();
        var front = new SpeechFront(voice, store);

        await front.ReadAsync(
            "hello",
            (_, _) => throw new InvalidOperationException("MP3 playback should not be selected."));

        await Assert.That(voice.SpokenText).IsEqualTo("hello");
        await Assert.That(front.Provider).IsEqualTo(SpeechProvider.DeviceVoice);
        await Assert.That(front.Capabilities.CanCreateMp3).IsFalse();
    }

    [Test]
    public async Task Azure_selection_requires_configuration()
    {
        var front = new SpeechFront(new CapturingVoice(), new MemoryTokenStore());

        await Assert.That(async () => await front.UseAzureSpeechAsync())
            .ThrowsExactly<SpeechCapabilityException>();
        await Assert.That(async () => await front.CreateMp3Async("hello"))
            .ThrowsExactly<SpeechCapabilityException>();
    }

    [Test]
    public async Task Azure_key_setup_is_persisted_and_enables_mp3_capability()
    {
        var store = new MemoryTokenStore();
        var front = new SpeechFront(new CapturingVoice(), store);

        await front.ConfigureAzureAsync(new AzureSpeechSetup
        {
            Endpoint = new Uri("https://speech.example.test/"),
            ApiKey = "secret",
            VoiceName = "en-US-AvaMultilingualNeural",
        });

        await Assert.That(front.Provider).IsEqualTo(SpeechProvider.AzureSpeech);
        await Assert.That(front.Capabilities.CanCreateMp3).IsTrue();
        await Assert.That(front.AzureConfiguration!.ApiKey).IsNull();
        await Assert.That(store.Values.Single()).Contains("secret");
    }

    [Test]
    public async Task Entra_setup_persists_client_configuration_without_a_secret()
    {
        var store = new MemoryTokenStore();
        var front = new SpeechFront(new CapturingVoice(), store);

        await front.ConfigureAzureAsync(new AzureSpeechSetup
        {
            Endpoint = new Uri("https://speech.example.test/"),
            AuthenticationMode = AzureSpeechAuthenticationMode.MicrosoftEntra,
            ClientId = "client-id",
            TenantId = "tenant-id",
        });

        await Assert.That(front.AzureConfiguration!.AuthenticationMode)
            .IsEqualTo(AzureSpeechAuthenticationMode.MicrosoftEntra);
        await Assert.That(front.AzureConfiguration.ApiKey).IsNull();
        await Assert.That(store.Values.Single()).Contains("client-id");
    }

    sealed class CapturingVoice : IVoiceService
    {
        public string? SpokenText { get; private set; }

        public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            SpokenText = text;
            return Task.CompletedTask;
        }

        public Task WriteToFileAsync(
            string text,
            FileInfo destination,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    sealed class MemoryTokenStore : ISecureTokenStore
    {
        readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Values => _values.Values;

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.GetValueOrDefault(key));

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
