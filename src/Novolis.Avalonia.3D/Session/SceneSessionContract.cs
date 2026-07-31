using Novolis.Agent.Core;
using Novolis.Agent.Surface;

namespace Novolis.Avalonia._3D.Session;

/// <summary>Attributed contract for the lightweight scene modeling agent surface.</summary>
[AgentSurface("scene",
    HttpPort = 18785,
    TcpPort = 18786,
    EnableEnv = "NOVOLIS_SCENE_SESSION",
    MarkerPrefix = "novolis-scene-session",
    Description = "CAD 3D scene editor (hierarchy, primitives, mesh edit, lights, cameras).")]
[AgentAction("new", Summary = "New empty scene")]
[AgentAction("open", Summary = "Open .nov3djson", Params = "path")]
[AgentAction("save", Summary = "Save .nov3djson", Params = "path?")]
[AgentAction("importmesh", Summary = "Import FBX/OBJ/glTF mesh via Assimp", Params = "path; name?; distance?=targetLengthMeters; parentId?")]
[AgentAction("select", Summary = "Select node", Params = "nodeId?")]
[AgentAction("delete", Summary = "Delete selection")]
[AgentAction("fit", Summary = "Fit view to scene")]
[AgentAction("addlight", Summary = "Place a typed light", Params = "lightKind|omni,spot,infinite,area; parentId?; intensity?; name?")]
[AgentAction("addcamera", Summary = "Place a camera", Params = "parentId?; name?")]
[AgentAction("addmesh", Summary = "Place a mesh primitive", Params = "primitive|box,sphere,plane,cylinder,cone,capsule,torus,pyramid,disc,tube,platonictetra,platonicocta,platonicicosa,platonicdodeca,landscape; name?; segments?")]
[AgentAction("addmaterial", Summary = "Add a material node", Params = "name?; materialColor?")]
[AgentAction("addgenerator", Summary = "Add Array or Symmetry", Params = "generatorKind|cloner,symmetry; sourceId?; count?; axis?")]
[AgentAction("addboole", Summary = "Add Boolean (Target/Cutter)", Params = "booleanKind|union,difference,intersection; targetId?; cutterId?")]
[AgentAction("setboole", Summary = "Edit Boolean params", Params = "nodeId; booleanKind?; targetId?; cutterId?")]
[AgentAction("addmodifier", Summary = "Add shaping modifier", Params = "modifierKind|weld,subdivision,optimize,bridge,extrude,bevel,inset,dissolve,knife; inputId?; distance?")]
[AgentAction("setlight", Summary = "Edit light properties", Params = "nodeId; lightKind?; intensity?; name?")]
[AgentAction("settransform", Summary = "Set node transform", Params = "nodeId; x?; y?; z?; rx?; ry?; rz?")]
[AgentAction("setactivecamera", Summary = "Set active look-through camera", Params = "nodeId")]
[AgentAction("matchviewport", Summary = "Move selected camera to match current viewport orbit", Params = "nodeId?; x?; y?; z?; rx?=targetX; ry?=targetY; rz?=targetZ; distance?=fovDeg")]
[AgentAction("seteditmode", Summary = "Object/Point/Edge/Polygon mode", Params = "editMode|object,point,edge,polygon")]
[AgentAction("setdisplaymode", Summary = "Viewport display", Params = "displayMode|wireframe,wirepoints,isoline")]
[AgentAction("makeeditable", Summary = "Bake mesh/generator to editable verts", Params = "nodeId?")]
[AgentAction("selectcomponents", Summary = "Select verts/edges/faces", Params = "indices; additive?; nodeId?")]
[AgentAction("moveselection", Summary = "Translate component selection", Params = "x?; y?; z?")]
[AgentAction("meshedit", Summary = "Selection-aware mesh op", Params = "modifierKind|extrude,bevel,inset,dissolve,knife,bridge,weld,optimize,subdivision; distance?")]
[AgentAction("dump", Summary = "Dump all artifacts (viewport PNG, window PNG, scene, mesh OBJ)", Params = "path?")]
[AgentAction("dumpall", Summary = "Alias of dump")]
[AgentAction("dumpviewport", Summary = "Dump viewport PNG only", Params = "path?")]
[AgentAction("dumpscene", Summary = "Dump scene .nov3djson copy", Params = "path?")]
[AgentAction("dumpmesh", Summary = "Dump mesh OBJ + stats", Params = "path?")]
[AgentAction("dumpwindow", Summary = "Dump window UI PNG", Params = "path?")]
public interface ISceneSession : IAgentHost;

public static class SceneSessionContract
{
    public static AgentSurfaceDefinition Definition { get; } = AgentSurfaceDefinition.From<ISceneSession>();
}

public static class SceneSessionActionIds
{
    public const string New = "new";
    public const string Open = "open";
    public const string Save = "save";
    public const string ImportMesh = "importmesh";
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
    public const string MatchViewport = "matchviewport";
    public const string SetEditMode = "seteditmode";
    public const string SetDisplayMode = "setdisplaymode";
    public const string MakeEditable = "makeeditable";
    public const string SelectComponents = "selectcomponents";
    public const string MoveSelection = "moveselection";
    public const string MeshEdit = "meshedit";
    public const string Dump = "dump";
    public const string DumpAll = "dumpall";
    public const string DumpViewport = "dumpviewport";
    public const string DumpScene = "dumpscene";
    public const string DumpMesh = "dumpmesh";
    public const string DumpWindow = "dumpwindow";
}
