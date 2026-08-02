using Novolis.Avalonia.Agent.Protocol;

namespace Novolis.Avalonia.Unit;

public sealed class AgentProtocolTests
{
    [Test]
    public async Task UiProtocolVersion_Current_IsSemverish()
    {
        await Assert.That(UiProtocolVersion.Current).IsEqualTo("1.2");
    }

    [Test]
    public async Task UiRpcMethodNames_AreStable()
    {
        await Assert.That(UiRpcMethodNames.Hello).IsEqualTo("ui.hello");
        await Assert.That(UiRpcMethodNames.Tree).IsEqualTo("ui.tree");
        await Assert.That(UiRpcMethodNames.Focus).IsEqualTo("ui.focus");
        await Assert.That(UiRpcMethodNames.Scroll).IsEqualTo("ui.scroll");
    }
}
