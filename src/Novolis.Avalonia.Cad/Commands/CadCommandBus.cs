using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

public sealed class CadCommandBus
{
    private readonly CadDocumentSession _session;
    private readonly Stack<ICadCommand> _undo = new();
    private readonly Stack<ICadCommand> _redo = new();

    public CadCommandBus(CadDocumentSession session) => _session = session;

    public CadDocumentSession Session => _session;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public event Action? Changed;

    public void Execute(ICadCommand command)
    {
        command.Execute(_session);
        _undo.Push(command);
        _redo.Clear();
        _session.MarkDirty();
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (_undo.Count == 0)
            return;
        var cmd = _undo.Pop();
        cmd.Undo(_session);
        _redo.Push(cmd);
        _session.MarkDirty();
        Changed?.Invoke();
    }

    public void Redo()
    {
        if (_redo.Count == 0)
            return;
        var cmd = _redo.Pop();
        cmd.Execute(_session);
        _undo.Push(cmd);
        _session.MarkDirty();
        Changed?.Invoke();
    }
}
