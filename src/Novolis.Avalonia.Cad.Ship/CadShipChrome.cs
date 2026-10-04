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

    /// <summary>
    /// Wires exterior hooks and registers <see cref="ImportShipActionId"/> on
    /// <paramref name="session"/>. Dispose the returned attachment when Ship mode
    /// is no longer active.
    /// </summary>
    public static IDisposable Attach(CadSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var hooks = new CadExteriorHooks
        {
            ShouldUse = CadShipExterior.ShouldUseExterior,
            Draw = CadShipExterior.Draw,
            HudLines = doc =>
                ("transport exterior", "Isolate ON = deck CAD · Isolate OFF = sealed freighter · MMB orbit"),
        };
        session.ExteriorHooks = hooks;

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

        return new Attachment(session, hooks);
    }

    private sealed class Attachment : IDisposable
    {
        private readonly CadSessionService _session;
        private readonly CadExteriorHooks _hooks;
        private bool _disposed;

        public Attachment(CadSessionService session, CadExteriorHooks hooks)
        {
            _session = session;
            _hooks = hooks;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _session.RemoveAction(ImportShipActionId);
            if (ReferenceEquals(_session.ExteriorHooks, _hooks))
                _session.ExteriorHooks = null;
        }
    }
}
