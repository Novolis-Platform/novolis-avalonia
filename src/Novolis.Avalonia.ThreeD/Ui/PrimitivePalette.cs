using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

public sealed class PrimitivePalette : WrapPanel
{
    public PrimitivePalette(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Margin = new Thickness(8, 4);
        foreach (MeshPrimitiveKind kind in Enum.GetValues<MeshPrimitiveKind>())
        {
            var k = kind;
            Children.Add(Chrome.Btn(Short(k), () => session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.AddMesh,
                Primitive = k.ToString().ToLowerInvariant(),
                Name = k.ToString(),
                Segments = k is MeshPrimitiveKind.Landscape ? 16 : 16,
            })));
        }
    }

    private static string Short(MeshPrimitiveKind k) => k switch
    {
        MeshPrimitiveKind.PlatonicTetra => "Tetra",
        MeshPrimitiveKind.PlatonicOcta => "Octa",
        MeshPrimitiveKind.PlatonicIcosa => "Icosa",
        MeshPrimitiveKind.PlatonicDodeca => "Dodeca",
        _ => k.ToString(),
    };
}
