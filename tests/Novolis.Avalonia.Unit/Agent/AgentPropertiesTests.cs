using Avalonia.Controls;
using Novolis.Avalonia.Agent;
using Novolis.Avalonia.Agent.Protocol;

namespace Novolis.Avalonia.Unit.Agent;

public sealed class AgentPropertiesTests
{
    [Test]
    public async Task InferRole_MapsCommonControls()
    {
        await Assert.That(AgentProperties.InferRole(new Button())).IsEqualTo(AgentRoleNames.Button);
        await Assert.That(AgentProperties.InferRole(new TextBox())).IsEqualTo(AgentRoleNames.TextBox);
        await Assert.That(AgentProperties.InferRole(new ListBox())).IsEqualTo(AgentRoleNames.ListBox);
        await Assert.That(AgentProperties.InferRole(new CheckBox())).IsEqualTo(AgentRoleNames.CheckBox);
    }

    [Test]
    public async Task SetId_AppliesRoleWhenProvided()
    {
        var button = new Button();
        AgentProperties.SetId(button, "submit", AgentRoleNames.Button);
        await Assert.That(AgentProperties.GetId(button)).IsEqualTo("submit");
        await Assert.That(AgentProperties.GetRole(button)).IsEqualTo(AgentRoleNames.Button);
    }

    [Test]
    public async Task SetId_WithNullRole_InfersRole()
    {
        var textBox = new TextBox();
        AgentProperties.SetId(textBox, "query", role: null);
        await Assert.That(AgentProperties.GetRole(textBox)).IsEqualTo(AgentRoleNames.TextBox);
    }
}
