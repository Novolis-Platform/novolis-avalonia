using Novolis.Avalonia.Agent.Protocol;

namespace Novolis.Avalonia.Unit;

public sealed class AgentProtocolTests
{
    [Test]
    public async Task UiProtocolVersion_Current_IsSemverish()
    {
        await Assert.That(UiProtocolVersion.Current).IsEqualTo("1.1");
    }

    [Test]
    public async Task UiRpcMethodNames_AreStable()
    {
        await Assert.That(UiRpcMethodNames.Hello).IsNotNull();
        await Assert.That(UiRpcMethodNames.Tree).IsNotNull();
    }
}
