using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

public sealed class DeleteEntitiesCommand : ICadCommand
{
    private readonly List<CadEntity> _removed = [];
    private readonly HashSet<Guid> _ids;

    public DeleteEntitiesCommand(IEnumerable<Guid> ids) => _ids = ids.ToHashSet();

    public string Label => "Delete";

    public void Execute(CadDocumentSession session)
    {
        _removed.Clear();
        foreach (var entity in session.Document.Entities.Where(e => _ids.Contains(e.Id)).ToList())
        {
            _removed.Add(entity);
            session.Document.Entities.Remove(entity);
        }

        if (session.SelectedId is { } sid && _ids.Contains(sid))
            session.SelectedId = null;
    }

    public void Undo(CadDocumentSession session)
    {
        foreach (var entity in _removed)
        {
            if (session.Document.Entities.All(e => e.Id != entity.Id))
                session.Document.Entities.Add(entity);
        }
    }
}
