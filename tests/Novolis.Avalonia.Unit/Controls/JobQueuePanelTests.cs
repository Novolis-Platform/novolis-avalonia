using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class JobQueuePanelTests
{
    [Test]
    public async Task JobQueueRow_Implements_Contract()
    {
        IJobQueueRow row = new JobQueueRow
        {
            Title = "Build PDF",
            StatusLabel = "Running",
            Detail = "chapter-3",
            LogTail = "ok",
            CanCancel = true,
            CanOpenOutput = true,
            Tag = "pdf"
        };
        await Assert.That(row.Title).IsEqualTo("Build PDF");
        await Assert.That(row.CanCancel).IsTrue();
        await Assert.That(row.Tag).IsEqualTo("pdf");
    }
}
