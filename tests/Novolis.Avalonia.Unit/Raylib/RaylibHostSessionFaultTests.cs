using Novolis.Avalonia.Raylib;
using Novolis.Raylib.Abstractions;
using Novolis.Raylib.Shell;

namespace Novolis.Avalonia.Unit.Raylib;

[NotInParallel("raylib-host-session")]
public sealed class RaylibHostSessionFaultTests
{
    [Test]
    public async Task Create_fault_is_recorded_and_does_not_kill_the_host_thread()
    {
        var previous = RaylibHostSession.CreateOnDemandHost;
        RaylibHostSession.CreateOnDemandHost = _ =>
            throw new InvalidOperationException("Timed out waiting for the Raylib GLFW lock.");
        try
        {
            using var session = new RaylibHostSession(
                new RaylibEmbeddedOptions { Width = 64, Height = 64, TargetFps = 15 },
                new NoopRenderer());
            session.Start();

            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (session.IsRunning && DateTime.UtcNow < deadline)
                Thread.Sleep(20);

            await Assert.That(session.IsRunning).IsFalse();
            await Assert.That(session.Fault).IsNotNull();
            await Assert.That(session.Fault!.Message).Contains("Raylib GLFW lock");
        }
        finally
        {
            RaylibHostSession.CreateOnDemandHost = previous;
        }
    }

    [Test]
    public async Task TargetInvocationException_is_unwrapped_as_the_glfw_fault()
    {
        var previous = RaylibHostSession.CreateOnDemandHost;
        RaylibHostSession.CreateOnDemandHost = _ =>
            throw new System.Reflection.TargetInvocationException(
                new InvalidOperationException("Timed out waiting for the Raylib GLFW lock."));
        try
        {
            using var session = new RaylibHostSession(
                new RaylibEmbeddedOptions { Width = 64, Height = 64, TargetFps = 15 },
                new NoopRenderer());
            session.Start();

            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (session.Fault is null && DateTime.UtcNow < deadline)
                Thread.Sleep(20);

            await Assert.That(session.Fault).IsTypeOf<InvalidOperationException>();
            await Assert.That(session.Fault!.Message).Contains("Raylib GLFW lock");
        }
        finally
        {
            RaylibHostSession.CreateOnDemandHost = previous;
        }
    }

    private sealed class NoopRenderer : IRaylibFrameRenderer
    {
        public void OnFrame(float deltaSeconds, int screenWidth, int screenHeight)
        {
        }
    }
}
