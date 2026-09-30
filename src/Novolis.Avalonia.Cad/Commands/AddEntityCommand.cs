using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

public sealed class AddEntityCommand : ICadCommand
{
    private readonly CadEntity _entity;

    public AddEntityCommand(CadEntity entity) => _entity = entity;

    public string Label => $"Add {_entity.Kind}";

    public void Execute(CadDocumentSession session)
    {
        _entity.WithLayer(session.Document);
        if (session.Document.Entities.All(e => e.Id != _entity.Id))
            session.Document.Entities.Add(_entity);
        session.SelectedId = _entity.Id;
    }

    public void Undo(CadDocumentSession session)
    {
        session.Document.Entities.RemoveAll(e => e.Id == _entity.Id);
        if (session.SelectedId == _entity.Id)
            session.SelectedId = null;
    }
}
