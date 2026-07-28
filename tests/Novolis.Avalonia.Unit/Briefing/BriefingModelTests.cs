using Novolis.Avalonia.Briefing;

namespace Novolis.Avalonia.Unit.Briefing;

public sealed class BriefingModelTests
{
    [Test]
    public async Task FeedLine_Formats_With_And_Without_Tag()
    {
        var plain = new FeedLine("vox.varr", "Watch Priority.");
        await Assert.That(plain.Display).IsEqualTo("[vox.varr] Watch Priority.");

        var tagged = new FeedLine("vox.ixa", "Ugly money.", "d18");
        await Assert.That(tagged.Display).IsEqualTo("d18 [vox.ixa] Ugly money.");
    }

    [Test]
    public async Task ScorecardRow_Filled_When_Hits_Positive()
    {
        var filled = new ScorecardRow("empty-berth", 1, "Formal plan failed");
        await Assert.That(filled.Filled).IsTrue();

        var empty = new ScorecardRow("claim", 0, "none", filled: false);
        await Assert.That(empty.Filled).IsFalse();
    }

    [Test]
    public async Task MetricRow_Holds_Key_Value_Note()
    {
        var row = new MetricRow("Ops liquid", "1200", "opening 1000");
        await Assert.That(row.Key).IsEqualTo("Ops liquid");
        await Assert.That(row.Value).IsEqualTo("1200");
        await Assert.That(row.Note).IsEqualTo("opening 1000");
    }
}
