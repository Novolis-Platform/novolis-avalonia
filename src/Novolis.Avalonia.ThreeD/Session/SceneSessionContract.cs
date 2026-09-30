using Novolis.Agent.Core;
using Novolis.Agent.Surface;

namespace Novolis.Avalonia.ThreeD.Session;

public static class SceneSessionContract
{
    public static AgentSurfaceDefinition Definition { get; } = AgentSurfaceDefinition.From<ISceneSession>();
}
