using Novolis.Avalonia.Live;

namespace Novolis.Avalonia.Unit.Live;

public sealed class LiveDemoCatalogTests
{
    [Test]
    public async Task CreateShowcase_Has_PulseBloom()
    {
        var docs = LiveDemoCatalog.CreateShowcase();

        await Assert.That(docs.Count).IsEqualTo(3);
        await Assert.That(docs[0].Id).IsEqualTo("pulse-bloom");
        await Assert.That(docs[0].Source).Contains("Program(");
    }

    [Test]
    public async Task DefaultBuffer_Mentions_NotePlay()
    {
        await Assert.That(LiveDemoCatalog.DefaultBuffer).Contains("Note.Play");
    }
}
