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
