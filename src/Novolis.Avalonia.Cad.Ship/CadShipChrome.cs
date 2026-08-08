using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Cad.Ship.Core;
using Novolis.Avalonia.Cad.Ship.Services;

namespace Novolis.Avalonia.Cad.Ship;

/// <summary>Registers freighter exterior + <c>importship</c> on a CAD session.</summary>
public static class CadShipChrome
{
    /// <summary>Action id for ship workspace import (same string as historical Cad session).</summary>
    public const string ImportShipActionId = "importship";

    /// <summary>Wires exterior hooks and registers <see cref="ImportShipActionId"/> on <paramref name="session"/>.</summary>
    public static void Attach(CadSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);

        CadExteriorHooks.ShouldUse = CadShipExterior.ShouldUseExterior;
        CadExteriorHooks.Draw = CadShipExterior.Draw;
        CadExteriorHooks.HudLines = doc =>
            ("transport exterior", "Isolate ON = deck CAD · Isolate OFF = sealed freighter · MMB orbit");

        session.RegisterAction(ImportShipActionId, command =>
        {
            try
            {
                var path = CadShipImport.ImportIntoWorkspace(session.Settings.DataRoot, command.Path);
                session.Document.OpenFromPath(path);
                session.Settings.Save();
                session.FitHandler?.Invoke();
                return new CadCommandResultDto
                {
                    ActionId = ImportShipActionId,
                    Ok = true,
                    Message = $"Imported ship ({session.Document.Document.Entities.Count} entities).",
                    Paths = [path],
                };
            }
            catch (Exception ex)
            {
                return new CadCommandResultDto
                {
                    ActionId = ImportShipActionId,
                    Ok = false,
                    Message = ex.Message,
                    ErrorCode = "importFailed",
                };
            }
        });
    }
}
