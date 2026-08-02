using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        await Assert.That(AgentProperties.InferRole(new ToggleButton())).IsEqualTo(AgentRoleNames.Toggle);
        await Assert.That(AgentProperties.InferRole(new ComboBox())).IsEqualTo(AgentRoleNames.ComboBox);
        await Assert.That(AgentProperties.InferRole(new TabControl())).IsEqualTo(AgentRoleNames.TabControl);
        await Assert.That(AgentProperties.InferRole(new Menu())).IsEqualTo(AgentRoleNames.Menu);
        await Assert.That(AgentProperties.InferRole(new Panel())).IsEqualTo(AgentRoleNames.Other);
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

    [Test]
    public async Task IgnoreProperty_RoundTrips()
    {
        var button = new Button();
        AgentProperties.SetIgnore(button, true);
        await Assert.That(AgentProperties.GetIgnore(button)).IsTrue();
        AgentProperties.SetIgnore(button, false);
        await Assert.That(AgentProperties.GetIgnore(button)).IsFalse();
    }

    [Test]
    public async Task SetRole_Directly()
    {
        var panel = new Panel();
        AgentProperties.SetRole(panel, AgentRoleNames.Other);
        await Assert.That(AgentProperties.GetRole(panel)).IsEqualTo(AgentRoleNames.Other);
    }

    [Test]
    public async Task SetId_PreservesExistingRole()
    {
        var button = new Button();
        AgentProperties.SetRole(button, AgentRoleNames.Toggle);
        AgentProperties.SetId(button, "mode", role: null);
        await Assert.That(AgentProperties.GetRole(button)).IsEqualTo(AgentRoleNames.Toggle);
    }
}
