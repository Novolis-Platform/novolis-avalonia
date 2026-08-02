using Novolis.Avalonia.Timeline;
using Novolis.Timeline;
using Novolis.Timeline.Presentation.GitGraph;
using TUnit.Core;

namespace Novolis.Avalonia.Unit.Timeline;

public sealed class GitGraphTimelineListTests
{
    [Test]
    [Skip("GitGraphTimelineList mutates ListBox.ItemsSource and requires an Avalonia UI thread; headless host not configured in this test project.")]
    public async Task SetRows_EmptyList_ClearsItemsSource()
    {
        var list = new GitGraphTimelineList();
        list.SetRows([SampleRow(isHere: true)]);
        list.SetRows([]);

        await Assert.That(list.ItemsSource is null).IsTrue();
        await Assert.That(list.SelectedItem is null).IsTrue();
    }

    [Test]
    [Skip("GitGraphTimelineList mutates ListBox.ItemsSource and requires an Avalonia UI thread; headless host not configured in this test project.")]
    public async Task SelectHeadRow_SelectsRowMarkedHere()
    {
        var rows = new[]
        {
            SampleRow(isHere: false),
            SampleRow(isHere: true),
        };
        var list = new GitGraphTimelineList();
        list.SetRows(rows);
        list.SelectHeadRow(rows);

        await Assert.That(list.SelectedGitRow).IsEqualTo(rows[1]);
    }

    [Test]
    [Skip("GitGraphTimelineList mutates ListBox.ItemsSource and requires an Avalonia UI thread; headless host not configured in this test project.")]
    public async Task SetRows_SkipsUpdateWhenEquivalent()
    {
        var rows = new[] { SampleRow(isHere: true) };
        var list = new GitGraphTimelineList();
        list.SetRows(rows);
        var firstSource = list.ItemsSource!;
        list.SetRows(rows.ToList());

        await Assert.That(list.ItemsSource!).IsSameReferenceAs(firstSource);
    }

    [Test]
    public async Task SampleRow_ConstructsGitGraphTimelineRow()
    {
        var row = SampleRow(isHere: true);
        await Assert.That(row.IsHere).IsTrue();
        await Assert.That(row.Subject).IsEqualTo("Initial commit");
    }

    private static GitGraphTimelineRow SampleRow(bool isHere) =>
        new(
            new TimelineNodeId(Guid.NewGuid()),
            "| * ",
            "Initial commit",
            "main",
            "snapshot",
            new GraphRgb(120, 180, 255),
            new GraphRgb(200, 200, 200),
            isHere,
            IsBranchPoint: false,
            "*",
            DateTimeOffset.UtcNow);
}
