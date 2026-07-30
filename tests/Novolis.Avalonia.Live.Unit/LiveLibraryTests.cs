using Novolis.Avalonia.Live;

namespace Novolis.Avalonia.Unit.Live;

public sealed class LiveDslCompletionProviderTests
{
    [Test]
    public async Task GetCompletions_Filters_By_Text()
    {
        var hits = LiveDslCompletionProvider.GetCompletions("Program").ToList();

        await Assert.That(hits.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(hits.Any(c => c.Text == "Program")).IsTrue();
    }

    [Test]
    public async Task GetCompletions_Empty_Filter_Returns_Catalog()
    {
        var hits = LiveDslCompletionProvider.GetCompletions(null).ToList();

        await Assert.That(hits.Count).IsGreaterThan(10);
    }
}

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

public sealed class LiveScriptCompilerTests
{
    [Test]
    public async Task CompileAsync_Repl_NotePlay_Succeeds()
    {
        var compiler = new LiveScriptCompiler();
        var program = await compiler.CompileAsync("Note.Play(C4)");

        await Assert.That(program).IsNotNull();
    }
}
