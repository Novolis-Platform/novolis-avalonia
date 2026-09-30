using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

/// <summary>Undoable field mutation (material, wall sides, …).</summary>
public sealed class MutateEntityFieldsCommand : ICadCommand
{
    private readonly Guid _id;
    private readonly Action<CadEntity> _apply;
    private readonly Action<CadEntity> _revert;
    private readonly string _label;

    public MutateEntityFieldsCommand(Guid id, string label, Action<CadEntity> apply, Action<CadEntity> revert)
    {
        _id = id;
        _label = label;
        _apply = apply;
        _revert = revert;
    }

    public string Label => _label;

    public void Execute(CadDocumentSession session)
    {
        var entity = session.Document.Entities.FirstOrDefault(e => e.Id == _id)
                     ?? throw new InvalidOperationException("Entity missing.");
        _apply(entity);
        session.SelectedId = _id;
    }

    public void Undo(CadDocumentSession session)
    {
        var entity = session.Document.Entities.FirstOrDefault(e => e.Id == _id);
        if (entity is null)
            return;
        _revert(entity);
        session.SelectedId = _id;
    }
}
