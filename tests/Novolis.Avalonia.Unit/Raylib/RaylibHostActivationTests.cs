using Novolis.Avalonia.Raylib;

namespace Novolis.Avalonia.Unit.Raylib;

public sealed class RaylibHostActivationTests
{
    [Test]
    public async Task Hidden_or_detached_control_must_not_start_the_process_host()
    {
        await Assert.That(RaylibHostActivation.ShouldStartHost(attachedToVisualTree: false, effectivelyVisible: true)).IsFalse();
        await Assert.That(RaylibHostActivation.ShouldStartHost(attachedToVisualTree: true, effectivelyVisible: false)).IsFalse();
        await Assert.That(RaylibHostActivation.ShouldStartHost(attachedToVisualTree: false, effectivelyVisible: false)).IsFalse();
    }

    [Test]
    public async Task Attached_visible_control_may_start_the_process_host()
    {
        await Assert.That(RaylibHostActivation.ShouldStartHost(attachedToVisualTree: true, effectivelyVisible: true)).IsTrue();
    }

    [Test]
    public async Task Hidden_host_control_does_not_start_glfw()
    {
        var host = new RaylibHostControl { IsVisible = false };
        host.EnsureHostStarted();
        await Assert.That(host.IsHostRunning).IsFalse();
        await Assert.That(host.LastHostError).IsNull();
    }
}
