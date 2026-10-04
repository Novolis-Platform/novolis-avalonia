using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Cad.Ship;
using Novolis.Avalonia.Ship.Services;
using Novolis.Avalonia.Ship.Ui;

namespace Novolis.Avalonia.Ship;

/// <summary>Registers Cad.Ship exterior/import plus ship validation and hatch helpers.</summary>
public static class ShipChrome
{
    public const string ValidateShipActionId = "validateship";
    public const string PlaceHatchActionId = "placehatch";
    public const string RefreshAirtightActionId = "refreshairtight";

    /// <summary>
    /// Attach freighter exterior hooks and ship-designer session actions.
    /// Dispose the returned attachment when Ship mode is no longer active.
    /// </summary>
    public static IDisposable Attach(CadSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cadAttachment = CadShipChrome.Attach(session);
        ShipSessionActions.Register(session);
        return new Attachment(session, cadAttachment);
    }

    private sealed class Attachment : IDisposable
    {
        private readonly CadSessionService _session;
        private readonly IDisposable _cadAttachment;
        private bool _disposed;

        public Attachment(CadSessionService session, IDisposable cadAttachment)
        {
            _session = session;
            _cadAttachment = cadAttachment;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ShipSessionActions.Unregister(_session);
            _cadAttachment.Dispose();
        }
    }

    /// <summary>Build a compact tool strip for Ship Designer (validate / airtight / deck).</summary>
    public static global::Avalonia.Controls.Control CreateToolStrip(
        CadSessionService session,
        Action<int>? onDeckChanged = null,
        Func<int>? getDeck = null) =>
        ShipToolStrip.Build(session, onDeckChanged, getDeck);
}
