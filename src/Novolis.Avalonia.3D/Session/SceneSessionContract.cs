using Novolis.Agent.Surface;

namespace Novolis.Avalonia._3D.Session;

/// <summary>Attributed contract for the CinemaLight scene agent surface.</summary>
[AgentSurface("scene",
    HttpPort = 18785,
    TcpPort = 18786,
    EnableEnv = "NOVOLIS_SCENE_SESSION",
    MarkerPrefix = "novolis-scene-session",
    Description = "CinemaLight scene modeling session (Object Manager + lights/cameras).")]
[AgentAction("new", Summary = "New empty scene")]
[AgentAction("open", Summary = "Open .nov3djson", Params = "path")]
[AgentAction("save", Summary = "Save .nov3djson", Params = "path?")]
[AgentAction("select", Summary = "Select node", Params = "nodeId?")]
[AgentAction("delete", Summary = "Delete selection")]
[AgentAction("fit", Summary = "Fit view to scene")]
[AgentAction("addlight", Summary = "Place a typed light", Params = "lightKind|omni,spot,infinite,area; parentId?; intensity?; name?")]
[AgentAction("addcamera", Summary = "Place a camera", Params = "parentId?; name?")]
[AgentAction("addmesh", Summary = "Place a mesh primitive", Params = "name?")]
[AgentAction("addmaterial", Summary = "Add a material node", Params = "name?; materialColor?")]
[AgentAction("addgenerator", Summary = "Add Cloner/Symmetry/Extrude", Params = "generatorKind|cloner,symmetry,extrude; sourceId?; count?; axis?")]
[AgentAction("addmodifier", Summary = "Add Weld/Subdivision/Optimize", Params = "modifierKind|weld,subdivision,optimize; inputId?")]
[AgentAction("setlight", Summary = "Edit light properties", Params = "nodeId; lightKind?; intensity?; name?")]
[AgentAction("settransform", Summary = "Set node transform", Params = "nodeId; x?; y?; z?; rx?; ry?; rz?")]
[AgentAction("setactivecamera", Summary = "Set active look-through camera", Params = "nodeId")]
public interface ISceneSession : IAgentSession;

public static class SceneSessionContract
{
    public static AgentSurfaceDefinition Definition { get; } = AgentSurfaceDefinition.From<ISceneSession>();
}

public static class SceneSessionActionIds
{
    public const string New = "new";
    public const string Open = "open";
    public const string Save = "save";
    public const string Select = "select";
    public const string Delete = "delete";
    public const string Fit = "fit";
    public const string AddLight = "addlight";
    public const string AddCamera = "addcamera";
    public const string AddMesh = "addmesh";
    public const string AddMaterial = "addmaterial";
    public const string AddGenerator = "addgenerator";
    public const string AddModifier = "addmodifier";
    public const string SetLight = "setlight";
    public const string SetTransform = "settransform";
    public const string SetActiveCamera = "setactivecamera";
}
