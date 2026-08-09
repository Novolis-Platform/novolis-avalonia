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

    /// <summary>Attach freighter exterior hooks and ship-designer session actions.</summary>
    public static void Attach(CadSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        CadShipChrome.Attach(session);
        ShipSessionActions.Register(session);
    }

    /// <summary>Build a compact tool strip for Ship Designer (validate / airtight / deck).</summary>
    public static global::Avalonia.Controls.Control CreateToolStrip(
        CadSessionService session,
        Action<int>? onDeckChanged = null,
        Func<int>? getDeck = null) =>
        ShipToolStrip.Build(session, onDeckChanged, getDeck);
}
