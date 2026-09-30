using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

/// <summary>Undoable geometry mutation for grip edits (line endpoints, box extents, …).</summary>
public sealed class MutateEntityGeometryCommand : ICadCommand
{
    private readonly Guid _id;
    private readonly EntityGeometrySnapshot _before;
    private readonly EntityGeometrySnapshot _after;

    public MutateEntityGeometryCommand(Guid id, EntityGeometrySnapshot before, EntityGeometrySnapshot after)
    {
        _id = id;
        _before = before;
        _after = after;
    }

    public string Label => "Edit geometry";

    public void Execute(CadDocumentSession session) => Apply(session, _after);

    public void Undo(CadDocumentSession session) => Apply(session, _before);

    private void Apply(CadDocumentSession session, EntityGeometrySnapshot snap)
    {
        var entity = session.Document.Entities.FirstOrDefault(e => e.Id == _id);
        if (entity is null)
            return;
        snap.ApplyTo(entity);
        session.SelectedId = _id;
    }
}
