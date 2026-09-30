using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

public interface ICadCommand
{
    string Label { get; }

    void Execute(CadDocumentSession session);

    void Undo(CadDocumentSession session);
}
