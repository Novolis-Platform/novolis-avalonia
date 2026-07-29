using Novolis.Agent.Surface;

namespace Novolis.Avalonia._3D.Session;

/// <summary>Attributed contract for the lightweight scene modeling agent surface.</summary>
[AgentSurface("scene",
    HttpPort = 18785,
    TcpPort = 18786,
    EnableEnv = "NOVOLIS_SCENE_SESSION",
    MarkerPrefix = "novolis-scene-session",
    Description = "C4D-inspired mesh modeling (Object Manager, primitives, wireframe poly edit, Look).")]
[AgentAction("new", Summary = "New empty scene")]
[AgentAction("open", Summary = "Open .nov3djson", Params = "path")]
[AgentAction("save", Summary = "Save .nov3djson", Params = "path?")]
[AgentAction("select", Summary = "Select node", Params = "nodeId?")]
[AgentAction("delete", Summary = "Delete selection")]
[AgentAction("fit", Summary = "Fit view to scene")]
[AgentAction("addlight", Summary = "Place a typed light", Params = "lightKind|omni,spot,infinite,area; parentId?; intensity?; name?")]
[AgentAction("addcamera", Summary = "Place a camera", Params = "parentId?; name?")]
[AgentAction("addmesh", Summary = "Place a mesh primitive", Params = "primitive|box,sphere,plane,cylinder,cone,capsule,torus,pyramid,disc,tube,platonictetra,platonicocta,platonicicosa,platonicdodeca,landscape; name?; segments?")]
[AgentAction("addmaterial", Summary = "Add a material node", Params = "name?; materialColor?")]
[AgentAction("addgenerator", Summary = "Add Array/Cloner or Symmetry", Params = "generatorKind|cloner,symmetry; sourceId?; count?; axis?")]
[AgentAction("addboole", Summary = "Add Boole (Target/Cutter)", Params = "booleanKind|union,difference,intersection; targetId?; cutterId?")]
[AgentAction("setboole", Summary = "Edit Boole params", Params = "nodeId; booleanKind?; targetId?; cutterId?")]
[AgentAction("addmodifier", Summary = "Add shaping modifier", Params = "modifierKind|weld,subdivision,optimize,bridge,extrude,bevel,inset,dissolve,knife; inputId?; distance?")]
[AgentAction("setlight", Summary = "Edit light properties", Params = "nodeId; lightKind?; intensity?; name?")]
[AgentAction("settransform", Summary = "Set node transform", Params = "nodeId; x?; y?; z?; rx?; ry?; rz?")]
[AgentAction("setactivecamera", Summary = "Set active look-through camera", Params = "nodeId")]
[AgentAction("seteditmode", Summary = "Object/Point/Edge/Polygon mode", Params = "editMode|object,point,edge,polygon")]
[AgentAction("setdisplaymode", Summary = "Viewport display", Params = "displayMode|wireframe,wirepoints,isoline")]
[AgentAction("makeeditable", Summary = "Bake mesh/generator to editable verts", Params = "nodeId?")]
[AgentAction("selectcomponents", Summary = "Select verts/edges/faces", Params = "indices; additive?; nodeId?")]
[AgentAction("moveselection", Summary = "Translate component selection", Params = "x?; y?; z?")]
[AgentAction("meshedit", Summary = "Selection-aware mesh op", Params = "modifierKind|extrude,bevel,inset,dissolve,knife,bridge,weld,optimize,subdivision; distance?")]
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
    public const string AddBoole = "addboole";
    public const string SetBoole = "setboole";
    public const string AddModifier = "addmodifier";
    public const string SetLight = "setlight";
    public const string SetTransform = "settransform";
    public const string SetActiveCamera = "setactivecamera";
    public const string SetEditMode = "seteditmode";
    public const string SetDisplayMode = "setdisplaymode";
    public const string MakeEditable = "makeeditable";
    public const string SelectComponents = "selectcomponents";
    public const string MoveSelection = "moveselection";
    public const string MeshEdit = "meshedit";
}
