using Novolis.Avalonia.Gaming;

namespace Novolis.Avalonia.Unit.Gaming;

public sealed class GameSimGateTests
{
    [Test]
    public async Task HardPause_Ticks_When_NotPaused_And_NoModal()
    {
        await Assert.That(GameSimGate.ShouldTick(GamePauseMode.HardPause, isPaused: false, modalOpen: false))
            .IsTrue();
    }

    [Test]
    public async Task HardPause_Freezes_When_Paused()
    {
        await Assert.That(GameSimGate.ShouldTick(GamePauseMode.HardPause, isPaused: true, modalOpen: false))
            .IsFalse();
    }

    [Test]
    public async Task HardPause_Freezes_When_ModalOpen()
    {
        await Assert.That(GameSimGate.ShouldTick(GamePauseMode.HardPause, isPaused: false, modalOpen: true))
            .IsFalse();
    }

    [Test]
    public async Task RunAlways_Ticks_Even_When_Paused_Or_Modal()
    {
        await Assert.That(GameSimGate.ShouldTick(GamePauseMode.RunAlways, isPaused: true, modalOpen: true))
            .IsTrue();
    }
}
