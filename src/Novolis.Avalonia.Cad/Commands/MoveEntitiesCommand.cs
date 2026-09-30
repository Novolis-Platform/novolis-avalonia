using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

public sealed class MoveEntitiesCommand : ICadCommand
{
    private readonly HashSet<Guid> _ids;
    private readonly float _dx;
    private readonly float _dy;
    private readonly float _dz;

    public MoveEntitiesCommand(IEnumerable<Guid> ids, float dx, float dy, float dz)
    {
        _ids = ids.ToHashSet();
        _dx = dx;
        _dy = dy;
        _dz = dz;
    }

    public string Label => "Move";

    public void Execute(CadDocumentSession session) => Apply(session, _dx, _dy, _dz);

    public void Undo(CadDocumentSession session) => Apply(session, -_dx, -_dy, -_dz);

    private void Apply(CadDocumentSession session, float dx, float dy, float dz)
    {
        foreach (var entity in session.Document.Entities.Where(e => _ids.Contains(e.Id)))
            CadVec.TranslateEntity(entity, dx, dy, dz);
    }
}
