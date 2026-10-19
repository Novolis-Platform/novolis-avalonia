using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class DownloadControlsTests
{
    [Test]
    public async Task DownloadItem_DerivesProgressFromByteCounts()
    {
        var item = new DownloadItem
        {
            Name = "sample.bin",
            TotalBytes = 200,
            CompletedBytes = 50,
        };

        await Assert.That(item.ProgressFraction).IsEqualTo(0.25);
    }

    [Test]
    public async Task DownloadItem_ClampsHostProgress()
    {
        var item = new DownloadItem
        {
            Name = "sample.bin",
            ProgressPercent = 120,
        };

        await Assert.That(item.ProgressFraction).IsEqualTo(1);
    }

    [Test]
    public async Task DownloadPeerGrid_ReplacesPeers()
    {
        var grid = new DownloadPeerGrid();
        var peer = new DownloadPeer
        {
            Endpoint = "source-1",
            PieceAvailability = [true, false, true],
        };

        grid.SetPeers([peer]);

        await Assert.That(grid.Peers.Count).IsEqualTo(1);
        await Assert.That(grid.Peers[0].Endpoint).IsEqualTo("source-1");
        await Assert.That(grid.SelectedPeer).IsNull();
    }
}
