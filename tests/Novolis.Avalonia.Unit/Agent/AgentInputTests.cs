using MessagePack;
using Novolis.Avalonia.Agent;
using Novolis.Avalonia.Agent.Protocol;
using Novolis.Avalonia.Agent.Protocol.Dto;

namespace Novolis.Avalonia.Unit.Agent;

public sealed class AgentInputTests
{
    [Test]
    public async Task ClickRequest_RoundTripsButtonAndClickCount()
    {
        var original = new UiClickRequestDto(7, "lab.btn", 1.5, 2.5, "right", 2);
        var bytes = MessagePackSerializer.Serialize(original);
        var copy = MessagePackSerializer.Deserialize<UiClickRequestDto>(bytes);
        await Assert.That(copy.Button).IsEqualTo("right");
        await Assert.That(copy.ClickCount).IsEqualTo(2);
        await Assert.That(copy.ControlId).IsEqualTo("lab.btn");
    }

    [Test]
    public async Task FocusAndScrollDtos_RoundTrip()
    {
        var focus = new UiFocusRequestDto(1, "lab.query");
        var focusCopy = MessagePackSerializer.Deserialize<UiFocusRequestDto>(MessagePackSerializer.Serialize(focus));
        await Assert.That(focusCopy.ControlId).IsEqualTo("lab.query");

        var scroll = new UiScrollRequestDto(2, "lab.scroll", 10, -40, true);
        var scrollCopy = MessagePackSerializer.Deserialize<UiScrollRequestDto>(MessagePackSerializer.Serialize(scroll));
        await Assert.That(scrollCopy.DeltaY).IsEqualTo(-40);
        await Assert.That(scrollCopy.BringIntoView).IsTrue();
    }

    [Test]
    public async Task FormatItemText_ReadsStringItems()
    {
        await Assert.That(AgentInput.FormatItemText("beta")).IsEqualTo("beta");
        await Assert.That(AgentInput.FormatItemText(null)).IsNull();
    }

    [Test]
    public async Task FormatTabHeader_ReadsStringHeaders()
    {
        await Assert.That(AgentInput.FormatTabHeader("Voyage")).IsEqualTo("Voyage");
        await Assert.That(AgentInput.FormatTabHeader(null)).IsNull();
    }

    [Test]
    public async Task Protocol_ExposesFocusAndScrollMethods()
    {
        await Assert.That(UiRpcMethodNames.Focus).IsEqualTo("ui.focus");
        await Assert.That(UiRpcMethodNames.Scroll).IsEqualTo("ui.scroll");
        await Assert.That(UiProtocolVersion.Current).IsEqualTo("1.2");
    }
}
