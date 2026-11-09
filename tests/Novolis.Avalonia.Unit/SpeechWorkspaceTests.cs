using Novolis.Audio.Voice;
using Novolis.Avalonia.Mobile;
using Novolis.Avalonia.Speech;

namespace Novolis.Avalonia.Unit;

public sealed class SpeechWorkspaceTests
{
    [Test]
    public async Task Initialize_selects_edge_when_azure_is_not_configured()
    {
        var front = new SpeechFront(new CapturingVoice(), new MemoryTokenStore());
        var workspace = new SpeechWorkspace(front);

        await workspace.InitializeAsync();

        await Assert.That(workspace.Provider).IsEqualTo(SpeechProvider.EdgeTts);
        await Assert.That(workspace.Capabilities.CanCreateMp3).IsTrue();
        await Assert.That(workspace.Voices.Count).IsGreaterThan(0);
        await Assert.That(workspace.WantAzureSetup).IsFalse();
    }

    [Test]
    public async Task Select_azure_without_credentials_opens_setup_intent()
    {
        var front = new SpeechFront(new CapturingVoice(), new MemoryTokenStore());
        var workspace = new SpeechWorkspace(front);
        await workspace.InitializeAsync();

        await workspace.SelectAzureAsync();

        await Assert.That(workspace.WantAzureSetup).IsTrue();
        await Assert.That(workspace.Provider).IsEqualTo(SpeechProvider.EdgeTts);
    }

    [Test]
    public async Task Manual_azure_connect_selects_azure_provider()
    {
        var store = new MemoryTokenStore();
        var front = new SpeechFront(new CapturingVoice(), store);
        var workspace = new SpeechWorkspace(front);
        await workspace.InitializeAsync();

        await workspace.ConnectManualAsync(
            new Uri("https://example.cognitiveservices.azure.com/"),
            "test-key-0123456789abcdef");

        await Assert.That(workspace.IsAzureConfigured).IsTrue();
        await Assert.That(workspace.Provider).IsEqualTo(SpeechProvider.AzureSpeech);
        await Assert.That(workspace.WantAzureSetup).IsFalse();
    }
}
