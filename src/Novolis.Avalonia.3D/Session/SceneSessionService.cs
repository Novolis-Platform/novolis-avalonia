using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Services;
using Novolis.Math.Geometry;
using Novolis.Modeling.Import;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Session;

/// <summary>Scene mutations for UI and agent transports.</summary>
public sealed class SceneSessionService : ISceneSession
{
    private readonly SceneEvaluator _evaluator = new();
    private SceneDocument _document;
    private string? _path;
    private AgentLastAction? _lastAction;
    private bool _subscribed;

    public SceneSessionService(SceneDocument? document = null)
    {
        _document = document ?? SceneDocument.CreateEmpty();
        _evaluator.Bind(_document);
        Definition = SceneSessionContract.Definition;
    }

    public AgentSurfaceDefinition Definition { get; }
    public string AppId { get; set; } = "avalonia-3d";
    public SceneDocument Document => _document;
    public SceneEvaluator Evaluator => _evaluator;
    public string? DocumentPath => _path;
    public int Revision { get; private set; }

    /// <summary>Shaded render preview settings (ambient, exposure, clear). Runtime only.</summary>
    public SceneRenderSettings RenderSettings { get; } = new();

    public event Action? DocumentChanged;
#pragma warning disable CS0067 // Scene editing has no decision gate to raise; kept for IAgentHost compliance.
    public event Action<AgentDecisionEvent>? Decision;
#pragma warning restore CS0067
    public event Action<AgentChangedEvent>? Changed;
    public event Action<AgentActionResultEvent>? ActionResult;
    /// <summary>Raised when dump/dumpall/dumpviewport/… is requested. Host should capture UI artifacts.</summary>
    public event Action<string>? DumpArtifactsRequested;
    /// <summary>Raised on Fit — hosts should frame the viewport camera.</summary>
    public event Action? FitRequested;
    /// <summary>Raised when the active camera should drive viewport look-through.</summary>
    public event Action? LookThroughRequested;
    /// <summary>Raised to open the shaded render window (host UI).</summary>
    public event Action? OpenShadeRenderRequested;
    /// <summary>Raised to save shaded render PNG; arg is optional path.</summary>
    public event Action<string?>? SaveRenderPngRequested;

    public AgentHello Hello() => Definition.BuildHello(AppId);

    public AgentSnapshot Snapshot() => new()
    {
        DocumentName = _document.Name,
        NodeCount = _document.Nodes.Count,
        SelectionId = _document.SelectionId?.ToString(),
        ActiveCameraId = _document.ActiveCameraId?.ToString(),
        LastAction = _lastAction,
        Document = _document,
        Actions = Actions().Actions,
    };

    public AgentActionsResponse Actions() => Definition.BuildActions(a =>
    {
        if (a.Id is SceneSessionActionIds.Delete or SceneSessionActionIds.SetLight or SceneSessionActionIds.SetTransform)
        {
            if (_document.SelectionId is null)
            {
                a.Enabled = false;
                a.DisabledReason = "noSelection";
            }
        }

        return a;
    });

    public AgentCommandResult Execute(AgentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = (command.ActionId ?? "").Trim().ToLowerInvariant();
        AgentCommandResult result;
        try
        {
            result = id switch
            {
                SceneSessionActionIds.New => DoNew(),
                SceneSessionActionIds.Open => DoOpen(command),
                SceneSessionActionIds.Save => DoSave(command),
                SceneSessionActionIds.ImportMesh => DoImportMesh(command),
                SceneSessionActionIds.ImportTriangles => DoImportTriangles(command),
                SceneSessionActionIds.DescribeScene => DoDescribeScene(),
                SceneSessionActionIds.GroundPhrase => DoGroundPhrase(command),
                SceneSessionActionIds.SetSceneProps => DoSetSceneProps(command),
                SceneSessionActionIds.Select => DoSelect(command),
                SceneSessionActionIds.Delete => DoDelete(),
                SceneSessionActionIds.Fit => DoFit(),
                SceneSessionActionIds.AddLight => DoAddLight(command),
                SceneSessionActionIds.AddCamera => DoAddCamera(command),
                SceneSessionActionIds.AddMesh => DoAddMesh(command),
                SceneSessionActionIds.AddMaterial => DoAddMaterial(command),
                SceneSessionActionIds.AddGenerator => DoAddGenerator(command),
                SceneSessionActionIds.AddBoole => DoAddBoole(command),
                SceneSessionActionIds.SetBoole => DoSetBoole(command),
                SceneSessionActionIds.AddModifier => DoAddModifier(command),
                SceneSessionActionIds.SetLight => DoSetLight(command),
                SceneSessionActionIds.SetTransform => DoSetTransform(command),
                SceneSessionActionIds.SetActiveCamera => DoSetActiveCamera(command),
                SceneSessionActionIds.MatchViewport => DoMatchViewport(command),
                SceneSessionActionIds.SetMeshMaterial => DoSetMeshMaterial(command),
                SceneSessionActionIds.EnsureStudioLights => DoEnsureStudioLights(),
                SceneSessionActionIds.OpenShadeRender => DoOpenShadeRender(command),
                SceneSessionActionIds.SaveRenderPng => DoSaveRenderPng(command),
                SceneSessionActionIds.SetEditMode => DoSetEditMode(command),
                SceneSessionActionIds.SetDisplayMode => DoSetDisplayMode(command),
                SceneSessionActionIds.MakeEditable => DoMakeEditable(command),
                SceneSessionActionIds.SelectComponents => DoSelectComponents(command),
                SceneSessionActionIds.MoveSelection => DoMoveSelection(command),
                SceneSessionActionIds.MeshEdit => DoMeshEdit(command),
                SceneSessionActionIds.Dump or SceneSessionActionIds.DumpAll => DoDump("all", command),
                SceneSessionActionIds.DumpViewport => DoDump("viewport", command),
                SceneSessionActionIds.DumpScene => DoDump("scene", command),
                SceneSessionActionIds.DumpMesh => DoDump("mesh", command),
                SceneSessionActionIds.DumpWindow => DoDump("window", command),
                _ => Fail(id, $"Unknown action '{command.ActionId}'.", "unknownAction"),
            };
        }
        catch (Exception ex)
        {
            result = Fail(id, ex.Message, "exception");
        }

        _lastAction = new AgentLastAction
        {
            ActionId = result.ActionId ?? id,
            Ok = result.Ok,
            Message = result.Message ?? "",
            ErrorCode = result.ErrorCode,
        };
        if (_subscribed)
            ActionResult?.Invoke(new AgentActionResultEvent
            {
                Ok = result.Ok,
                ActionId = result.ActionId ?? "",
                Message = result.Message ?? "",
            });
        return result;
    }

    /// <summary>Scene editing has no decision gate; always succeeds as a no-op.</summary>
    public AgentCommandResult Continue() => Ok("continue", "No decision gate to release.");

    public void Subscribe() => _subscribed = true;

    public void ReplaceDocument(SceneDocument document, string? path = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _path = path;
        _evaluator.Bind(_document);
        RaiseChanged("replace");
    }

    private AgentCommandResult DoFit()
    {
        FitRequested?.Invoke();
        return Ok(SceneSessionActionIds.Fit, "Fit.");
    }

    private AgentCommandResult DoDump(string kind, AgentCommand command)
    {
        // Optional Path overrides host dump root when the UI handler reads command.Path.
        DumpArtifactsRequested?.Invoke(string.IsNullOrWhiteSpace(command.Path) ? kind : $"{kind}|{command.Path}");
        return Ok(command.ActionId ?? kind, $"Dump '{kind}' requested.");
    }

    private AgentCommandResult DoNew()
    {
        ReplaceDocument(SceneDocument.CreateEmpty());
        return Ok(SceneSessionActionIds.New, "New scene.");
    }

    private AgentCommandResult DoOpen(AgentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Path))
            return Fail(SceneSessionActionIds.Open, "path required.", "badPath");
        var doc = SceneSerializer.Load(command.Path);
        ReplaceDocument(doc, command.Path);
        return Ok(SceneSessionActionIds.Open, $"Opened {Path.GetFileName(command.Path)}.");
    }

    private AgentCommandResult DoSave(AgentCommand command)
    {
        var path = command.Path ?? _path;
        if (string.IsNullOrWhiteSpace(path))
            return Fail(SceneSessionActionIds.Save, "path required.", "badPath");
        SceneSerializer.Save(_document, path);
        _path = path;
        RaiseChanged("save");
        return Ok(SceneSessionActionIds.Save, $"Saved {Path.GetFileName(path)}.");
    }

    private AgentCommandResult DoImportMesh(AgentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Path))
            return Fail(SceneSessionActionIds.ImportMesh, "path required.", "badPath");
        if (!File.Exists(command.Path))
            return Fail(SceneSessionActionIds.ImportMesh, "file not found.", "missingFile");

        EditableMesh editable;
        try
        {
            editable = AssimpMeshImporter.ImportEditable(command.Path, new MeshImportOptions
            {
                TargetLengthMeters = command.Distance is > 0f ? command.Distance : null,
                CenterAtOrigin = true,
                LongestAxisToPositiveZ = command.Distance is > 0f,
                PreTransformVertices = true,
            });
        }
        catch (Exception ex)
        {
            return Fail(SceneSessionActionIds.ImportMesh, ex.Message, "importFailed");
        }

        var name = string.IsNullOrWhiteSpace(command.Name)
            ? Path.GetFileNameWithoutExtension(command.Path)
            : command.Name!;
        var mesh = new MeshNode
        {
            Name = name,
            ParentId = ResolveParent(command.ParentId),
            Primitive = MeshPrimitiveKind.Box,
            Transform = new SceneTransform
            {
                Position = [command.X ?? 0f, command.Y ?? 0f, command.Z ?? 0f],
            },
        };
        MeshEditBake.WriteBaked(mesh, editable);
        _document.Nodes.Add(mesh);
        _document.SelectionId = mesh.Id;
        _evaluator.NotifyNodeChanged(mesh);
        RaiseChanged("importmesh");
        return Ok(
            SceneSessionActionIds.ImportMesh,
            $"Imported {name} ({editable.VertexCount} verts / {editable.TriangleCount} tris).",
            mesh.Id.ToString());
    }

    private AgentCommandResult DoImportTriangles(AgentCommand command)
    {
        if (!TryResolveTriangleSoup(command, out var verts, out var indices, out var nameHint, out var error))
            return Fail(SceneSessionActionIds.ImportTriangles, error, "badSoup");

        EditableMesh editable;
        try
        {
            var positions = new Vector3[verts.Length / 3];
            for (var i = 0; i < positions.Length; i++)
                positions[i] = new Vector3(verts[i * 3], verts[i * 3 + 1], verts[i * 3 + 2]);
            editable = new EditableMesh(positions, indices);
            FrameEditableMesh(editable, command.Distance is > 0f ? command.Distance : null);
        }
        catch (Exception ex)
        {
            return Fail(SceneSessionActionIds.ImportTriangles, ex.Message, "importFailed");
        }

        var name = string.IsNullOrWhiteSpace(command.Name)
            ? (string.IsNullOrWhiteSpace(nameHint) ? "Triangles" : nameHint!)
            : command.Name!;
        var mesh = new MeshNode
        {
            Name = name,
            ParentId = ResolveParent(command.ParentId),
            Primitive = MeshPrimitiveKind.Box,
            Transform = new SceneTransform
            {
                Position = [command.X ?? 0f, command.Y ?? 0f, command.Z ?? 0f],
            },
        };
        MeshEditBake.WriteBaked(mesh, editable);
        _document.Nodes.Add(mesh);
        _document.SelectionId = mesh.Id;
        _evaluator.NotifyNodeChanged(mesh);
        RaiseChanged("importtriangles");
        return Ok(
            SceneSessionActionIds.ImportTriangles,
            $"Imported {name} ({editable.VertexCount} verts / {editable.TriangleCount} tris).",
            mesh.Id.ToString());
    }

    private AgentCommandResult DoDescribeScene()
    {
        var byKind = _document.Nodes
            .GroupBy(NodeKindLabel)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        string? selectionName = null;
        if (_document.SelectionId is { } sid && _document.Find(sid) is { } sel)
            selectionName = sel.Name;

        string? cameraName = null;
        if (_document.ActiveCameraId is { } cid && _document.Find(cid) is { } cam)
            cameraName = cam.Name;

        var roots = _document.Roots()
            .Select(n => new { id = n.Id.ToString(), name = n.Name, kind = NodeKindLabel(n) })
            .ToList();

        var nodes = _document.Nodes
            .Select(n => new
            {
                id = n.Id.ToString(),
                name = n.Name,
                kind = NodeKindLabel(n),
                parentId = n.ParentId?.ToString(),
            })
            .ToList();

        var payload = new
        {
            name = _document.Name,
            unitScaleMeters = _document.UnitScaleMeters,
            nodeCount = _document.Nodes.Count,
            countsByKind = byKind,
            selectionId = _document.SelectionId?.ToString(),
            selectionName,
            activeCameraId = _document.ActiveCameraId?.ToString(),
            activeCameraName = cameraName,
            properties = _document.Properties ?? new Dictionary<string, string>(),
            roots,
            nodes,
            path = _path,
        };

        var json = JsonSerializer.Serialize(payload);
        return Ok(SceneSessionActionIds.DescribeScene, json);
    }

    private AgentCommandResult DoGroundPhrase(AgentCommand command)
    {
        var phrase = command.Phrase ?? command.Get("phrase");
        if (string.IsNullOrWhiteSpace(phrase))
            return Fail(SceneSessionActionIds.GroundPhrase, "phrase required.", "badPhrase");

        var needle = phrase.Trim();
        var hits = _document.Nodes
            .Select(n =>
            {
                var idStr = n.Id.ToString();
                var score = RankPhrase(needle, n.Name, idStr);
                return score <= 0
                    ? null
                    : new { id = idStr, name = n.Name, kind = NodeKindLabel(n), score };
            })
            .Where(h => h is not null)
            .Select(h => h!)
            .OrderByDescending(h => h.score)
            .ThenBy(h => h.name, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        var select = true;
        if (command.Select is bool typedSelect)
            select = typedSelect;
        else if (command.TryGetBool("select", out var selectFlag))
            select = selectFlag;

        string? bestId = null;
        if (hits.Count > 0)
        {
            bestId = hits[0].id;
            if (select && Guid.TryParse(bestId, out var gid))
            {
                _document.SelectionId = gid;
                RaiseChanged("groundphrase");
            }
        }

        var payload = new
        {
            phrase = needle,
            hitCount = hits.Count,
            selected = select && bestId is not null,
            hits,
        };
        return Ok(SceneSessionActionIds.GroundPhrase, JsonSerializer.Serialize(payload), bestId);
    }

    private AgentCommandResult DoSetSceneProps(AgentCommand command)
    {
        var key = command.Key ?? command.Get("key");
        if (string.IsNullOrWhiteSpace(key))
            return Fail(SceneSessionActionIds.SetSceneProps, "key required.", "badKey");

        var value = command.Value ?? command.Get("value");
        _document.Properties ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(value))
        {
            _document.Properties.Remove(key);
            if (_document.Properties.Count == 0)
                _document.Properties = null;
            RaiseChanged("setsceneprops");
            return Ok(SceneSessionActionIds.SetSceneProps, $"Cleared property '{key}'.");
        }

        _document.Properties[key] = value;
        RaiseChanged("setsceneprops");
        return Ok(SceneSessionActionIds.SetSceneProps, $"Set property '{key}'.");
    }

    private AgentCommandResult DoSelect(AgentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.NodeId))
        {
            _document.SelectionId = null;
            RaiseChanged("select");
            return Ok(SceneSessionActionIds.Select, "Cleared selection.");
        }

        if (!Guid.TryParse(command.NodeId, out var id) || _document.Find(id) is null)
            return Fail(SceneSessionActionIds.Select, "Unknown node.", "badNode");
        _document.SelectionId = id;
        RaiseChanged("select");
        return Ok(SceneSessionActionIds.Select, $"Selected {id}.");
    }

    private AgentCommandResult DoDelete()
    {
        if (_document.SelectionId is not { } id)
            return Fail(SceneSessionActionIds.Delete, "Nothing selected.", "noSelection");
        if (!_document.TryRemove(id))
            return Fail(SceneSessionActionIds.Delete, "Not found.", "badNode");
        _evaluator.InvalidateAll();
        RaiseChanged("delete");
        return Ok(SceneSessionActionIds.Delete, "Deleted.");
    }

    private AgentCommandResult DoAddLight(AgentCommand command)
    {
        var kind = ParseLightKind(command.LightKind);
        var parent = ResolveParent(command.ParentId);
        var light = new LightNode
        {
            Name = string.IsNullOrWhiteSpace(command.Name) ? $"{kind} Light" : command.Name!,
            ParentId = parent,
            LightKind = kind,
            Intensity = command.Intensity ?? 1.5f,
            Transform = new SceneTransform
            {
                Position =
                [
                    command.X ?? 2f,
                    command.Y ?? 3f,
                    command.Z ?? 2f,
                ],
            },
        };
        _document.Nodes.Add(light);
        _document.SelectionId = light.Id;
        _evaluator.NotifyNodeChanged(light);
        RaiseChanged("addlight");
        return Ok(SceneSessionActionIds.AddLight, $"Added {kind}.", light.Id.ToString());
    }

    private AgentCommandResult DoAddCamera(AgentCommand command)
    {
        var cam = new CameraNode
        {
            Name = string.IsNullOrWhiteSpace(command.Name) ? "Camera" : command.Name!,
            ParentId = ResolveParent(command.ParentId),
            Transform = new SceneTransform
            {
                Position = [command.X ?? 4f, command.Y ?? 3f, command.Z ?? 6f],
            },
        };
        _document.Nodes.Add(cam);
        _document.ActiveCameraId ??= cam.Id;
        _document.SelectionId = cam.Id;
        _evaluator.NotifyNodeChanged(cam);
        RaiseChanged("addcamera");
        return Ok(SceneSessionActionIds.AddCamera, "Added camera.", cam.Id.ToString());
    }

    private AgentCommandResult DoAddMesh(AgentCommand command)
    {
        var primitive = Enum.TryParse<MeshPrimitiveKind>(command.Primitive, ignoreCase: true, out var p)
            ? p
            : MeshPrimitiveKind.Box;
        var mesh = new MeshNode
        {
            Name = string.IsNullOrWhiteSpace(command.Name) ? primitive.ToString() : command.Name!,
            ParentId = ResolveParent(command.ParentId),
            Primitive = primitive,
            Segments = command.Segments ?? 16,
            Transform = new SceneTransform
            {
                Position = [command.X ?? 0f, command.Y ?? 0.5f, command.Z ?? 0f],
            },
        };
        _document.Nodes.Add(mesh);
        _document.SelectionId = mesh.Id;
        _evaluator.NotifyNodeChanged(mesh);
        RaiseChanged("addmesh");
        return Ok(SceneSessionActionIds.AddMesh, $"Added {primitive}.", mesh.Id.ToString());
    }

    private AgentCommandResult DoAddMaterial(AgentCommand command)
    {
        var mat = new MaterialNode
        {
            Name = string.IsNullOrWhiteSpace(command.Name) ? "Material" : command.Name!,
            ParentId = ResolveParent(command.ParentId),
        };
        if (!string.IsNullOrWhiteSpace(command.MaterialColor)
            && command.MaterialColor!.StartsWith('#')
            && command.MaterialColor.Length >= 7)
        {
            mat.Color =
            [
                Convert.ToInt32(command.MaterialColor[1..3], 16) / 255f,
                Convert.ToInt32(command.MaterialColor[3..5], 16) / 255f,
                Convert.ToInt32(command.MaterialColor[5..7], 16) / 255f,
            ];
        }

        _document.Nodes.Add(mat);
        _document.SelectionId = mat.Id;
        _evaluator.NotifyNodeChanged(mat);
        RaiseChanged("addmaterial");
        return Ok(SceneSessionActionIds.AddMaterial, "Added material.", mat.Id.ToString());
    }

    private AgentCommandResult DoAddGenerator(AgentCommand command)
    {
        var kind = Enum.TryParse<GeneratorKind>(command.GeneratorKind, ignoreCase: true, out var g)
            ? g
            : GeneratorKind.Cloner;
        if (kind == GeneratorKind.Boole)
            return DoAddBoole(command);

        Guid? sourceId = null;
        if (!string.IsNullOrWhiteSpace(command.SourceId) && Guid.TryParse(command.SourceId, out var sid))
            sourceId = sid;
        else if (_document.SelectionId is { } sel && _document.Find(sel) is MeshNode)
            sourceId = sel;

        var gen = new GeneratorNode
        {
            Name = kind == GeneratorKind.Cloner ? "Array" : kind.ToString(),
            ParentId = ResolveParent(command.ParentId),
            Generator = kind,
            SourceId = sourceId,
            Count = command.Count ?? 3,
            Axis = command.Axis ?? "x",
        };
        _document.Nodes.Add(gen);
        _document.SelectionId = gen.Id;
        _evaluator.InvalidateMesh();
        RaiseChanged("addgenerator");
        return Ok(SceneSessionActionIds.AddGenerator, $"Added {kind}.", gen.Id.ToString());
    }

    private AgentCommandResult DoAddBoole(AgentCommand command)
    {
        Guid? targetId = ParseGuid(command.TargetId) ?? ParseGuid(command.SourceId);
        Guid? cutterId = ParseGuid(command.CutterId);
        if (targetId is null && _document.SelectionId is { } sel && _document.Find(sel) is MeshNode)
            targetId = sel;
        if (cutterId is null)
        {
            cutterId = _document.Nodes.OfType<MeshNode>()
                .Select(m => m.Id)
                .FirstOrDefault(id => id != targetId);
            if (cutterId == Guid.Empty)
                cutterId = null;
        }

        var booleanKind = Enum.TryParse<BooleanKind>(command.BooleanKind, ignoreCase: true, out var bk)
            ? bk
            : BooleanKind.Difference;

        var gen = new GeneratorNode
        {
            Name = $"Boolean {booleanKind}",
            ParentId = ResolveParent(command.ParentId),
            Generator = GeneratorKind.Boole,
            TargetId = targetId,
            CutterId = cutterId,
            SourceId = targetId,
            BooleanKind = booleanKind,
        };
        _document.Nodes.Add(gen);
        _document.SelectionId = gen.Id;
        _evaluator.InvalidateMesh();
        RaiseChanged("addboole");
        return Ok(SceneSessionActionIds.AddBoole, $"Added Boolean {booleanKind}.", gen.Id.ToString());
    }

    private AgentCommandResult DoSetBoole(AgentCommand command)
    {
        if (!TryGetSelectedOrNode(command.NodeId, out var node) || node is not GeneratorNode { Generator: GeneratorKind.Boole } gen)
            return Fail(SceneSessionActionIds.SetBoole, "Select a Boolean generator.", "badNode");
        if (!string.IsNullOrWhiteSpace(command.BooleanKind)
            && Enum.TryParse<BooleanKind>(command.BooleanKind, ignoreCase: true, out var bk))
            gen.BooleanKind = bk;
        if (ParseGuid(command.TargetId) is { } tid)
            gen.TargetId = tid;
        if (ParseGuid(command.CutterId) is { } cid)
            gen.CutterId = cid;
        _evaluator.InvalidateMesh();
        RaiseChanged("setboole");
        return Ok(SceneSessionActionIds.SetBoole, "Boolean updated.", gen.Id.ToString());
    }

    private AgentCommandResult DoAddModifier(AgentCommand command)
    {
        var kind = Enum.TryParse<ModifierKind>(command.ModifierKind, ignoreCase: true, out var m)
            ? m
            : ModifierKind.Weld;
        Guid? inputId = ParseGuid(command.InputId);
        if (inputId is null && _document.SelectionId is { } sel)
            inputId = sel;

        var mod = new ModifierNode
        {
            Name = kind.ToString(),
            ParentId = ResolveParent(command.ParentId),
            Modifier = kind,
            InputId = inputId,
            Distance = command.Distance ?? (kind is ModifierKind.Extrude or ModifierKind.Bevel ? 0.2f : 0.001f),
            Levels = command.Count ?? 1,
        };
        _document.Nodes.Add(mod);
        _document.SelectionId = mod.Id;
        _evaluator.InvalidateMesh();
        RaiseChanged("addmodifier");
        return Ok(SceneSessionActionIds.AddModifier, $"Added {kind}.", mod.Id.ToString());
    }

    private static Guid? ParseGuid(string? raw) =>
        !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var id) ? id : null;

    private AgentCommandResult DoSetLight(AgentCommand command)
    {
        if (!TryGetSelectedOrNode(command.NodeId, out var node) || node is not LightNode light)
            return Fail(SceneSessionActionIds.SetLight, "Select a light.", "badNode");
        if (!string.IsNullOrWhiteSpace(command.LightKind))
            light.LightKind = ParseLightKind(command.LightKind);
        if (command.Intensity is { } intensity)
            light.Intensity = intensity;
        if (!string.IsNullOrWhiteSpace(command.Name))
            light.Name = command.Name!;
        _evaluator.NotifyNodeChanged(light);
        RaiseChanged("setlight");
        return Ok(SceneSessionActionIds.SetLight, "Light updated.", light.Id.ToString());
    }

    private AgentCommandResult DoSetTransform(AgentCommand command)
    {
        if (!TryGetSelectedOrNode(command.NodeId, out var node) || node is null)
            return Fail(SceneSessionActionIds.SetTransform, "Select a node.", "badNode");
        if (command.X is { } x) node.Transform.Position[0] = x;
        if (command.Y is { } y) node.Transform.Position[1] = y;
        if (command.Z is { } z) node.Transform.Position[2] = z;
        if (command.Rx is { } rx) node.Transform.RotationDeg[0] = rx;
        if (command.Ry is { } ry) node.Transform.RotationDeg[1] = ry;
        if (command.Rz is { } rz) node.Transform.RotationDeg[2] = rz;
        _evaluator.NotifyNodeChanged(node);
        RaiseChanged("settransform");
        return Ok(SceneSessionActionIds.SetTransform, "Transform updated.", node.Id.ToString());
    }


    private AgentCommandResult DoMatchViewport(AgentCommand command)
    {
        CameraNode? cam = null;
        if (!string.IsNullOrWhiteSpace(command.NodeId) && Guid.TryParse(command.NodeId, out var nid))
            cam = _document.Find(nid) as CameraNode;
        cam ??= _document.ActiveCameraId is { } aid ? _document.Find(aid) as CameraNode : null;
        cam ??= _document.SelectionId is { } sid ? _document.Find(sid) as CameraNode : null;
        if (cam is null)
            return Fail(SceneSessionActionIds.MatchViewport, "Camera node required.", "badNode");

        if (command.X is not float x || command.Y is not float y || command.Z is not float z)
            return Fail(SceneSessionActionIds.MatchViewport, "Viewport eye (x,y,z) required.", "badArgs");

        cam.Transform.Position = [x, y, z];
        if (command.Rx is float tx && command.Ry is float ty && command.Rz is float tz)
            cam.Target = [tx, ty, tz];
        if (command.Distance is float fov && fov is > 1f and < 170f)
            cam.FovDeg = fov;

        var eye = new System.Numerics.Vector3(x, y, z);
        var target = new System.Numerics.Vector3(cam.Target[0], cam.Target[1], cam.Target[2]);
        var forward = target - eye;
        if (forward.LengthSquared() > 1e-8f)
        {
            forward = System.Numerics.Vector3.Normalize(forward);
            var yaw = MathF.Atan2(forward.X, forward.Z) * (180f / MathF.PI);
            var pitch = MathF.Asin(System.Math.Clamp(forward.Y, -1f, 1f)) * (180f / MathF.PI);
            cam.Transform.RotationDeg = [-pitch, yaw, 0f];
        }

        _document.ActiveCameraId = cam.Id;
        _document.SelectionId = cam.Id;
        _evaluator.NotifyNodeChanged(cam);
        RaiseChanged("matchviewport");
        DocumentChanged?.Invoke();
        return Ok(SceneSessionActionIds.MatchViewport, "Camera matched viewport.", cam.Id.ToString());
    }

    private AgentCommandResult DoSetMeshMaterial(AgentCommand command)
    {
        if (!Guid.TryParse(command.NodeId, out var meshId) || _document.Find(meshId) is not MeshNode mesh)
            return Fail(SceneSessionActionIds.SetMeshMaterial, "Mesh nodeId required.", "badNode");
        var matRaw = command.TargetId ?? command.Get("materialId");
        if (string.IsNullOrWhiteSpace(matRaw) || !Guid.TryParse(matRaw, out var matId)
            || _document.Find(matId) is not MaterialNode)
            return Fail(SceneSessionActionIds.SetMeshMaterial, "materialId / targetId must be a MaterialNode.", "badMaterial");
        mesh.MaterialId = matId;
        _evaluator.NotifyNodeChanged(mesh);
        RaiseChanged("setmeshmaterial");
        DocumentChanged?.Invoke();
        return Ok(SceneSessionActionIds.SetMeshMaterial, "Material bound.", mesh.Id.ToString());
    }

    private AgentCommandResult DoEnsureStudioLights()
    {
        var lights = _document.Nodes.OfType<LightNode>().ToList();
        void Add(string name, LightKind kind, float intensity, float x, float y, float z, float rx, float ry, float rz)
        {
            if (lights.Any(l => l.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                return;
            var light = new LightNode
            {
                Name = name,
                LightKind = kind,
                Intensity = intensity,
                Transform = new SceneTransform
                {
                    Position = [x, y, z],
                    RotationDeg = [rx, ry, rz],
                },
            };
            _document.Nodes.Add(light);
        }

        Add("Key", LightKind.Spot, 3.8f, 22f, 16f, 18f, 40f, -35f, 0f);
        Add("Fill", LightKind.Omni, 2f, 0f, 2f, -18f, 0f, 0f, 0f);
        Add("Rim", LightKind.Infinite, 0.55f, 0f, 0f, 0f, -55f, 30f, 0f);
        RaiseChanged("ensurestudiolights");
        DocumentChanged?.Invoke();
        return Ok(SceneSessionActionIds.EnsureStudioLights, "Studio lights ensured.");
    }

    private AgentCommandResult DoOpenShadeRender(AgentCommand command)
    {
        _ = command;
        OpenShadeRenderRequested?.Invoke();
        return Ok(SceneSessionActionIds.OpenShadeRender, "Open shaded render requested.");
    }

    private AgentCommandResult DoSaveRenderPng(AgentCommand command)
    {
        SaveRenderPngRequested?.Invoke(command.Path);
        return Ok(SceneSessionActionIds.SaveRenderPng, command.Path ?? "Save render PNG requested.");
    }

    private AgentCommandResult DoSetActiveCamera(AgentCommand command)
    {
        if (!Guid.TryParse(command.NodeId, out var id) || _document.Find(id) is not CameraNode)
            return Fail(SceneSessionActionIds.SetActiveCamera, "Camera node required.", "badNode");
        _document.ActiveCameraId = id;
        RaiseChanged("setactivecamera");
        DocumentChanged?.Invoke();
        LookThroughRequested?.Invoke();
        return Ok(SceneSessionActionIds.SetActiveCamera, "Active camera set.", id.ToString());
    }

    private AgentCommandResult DoSetEditMode(AgentCommand command)
    {
        if (!Enum.TryParse<SceneEditMode>(command.EditMode, ignoreCase: true, out var mode))
            return Fail(SceneSessionActionIds.SetEditMode, "editMode required.", "badMode");

        if (mode is SceneEditMode.Point or SceneEditMode.Edge or SceneEditMode.Polygon)
        {
            var target = ResolveEditTarget(command.NodeId);
            if (target is null)
                return Fail(SceneSessionActionIds.SetEditMode, "Select a mesh first.", "noSelection");
            if (!MeshEditBake.MakeEditable(_document, _evaluator, target.Value))
                return Fail(SceneSessionActionIds.SetEditMode, "Could not make editable.", "badNode");
            _document.Edit.EditMeshId = target;
            _document.SelectionId = target;
        }

        _document.Edit.Mode = mode;
        if (mode == SceneEditMode.Object)
            _document.Edit.ClearComponents();
        RaiseChanged("seteditmode");
        return Ok(SceneSessionActionIds.SetEditMode, $"Edit mode {mode}.");
    }

    private AgentCommandResult DoSetDisplayMode(AgentCommand command)
    {
        if (!Enum.TryParse<SceneDisplayMode>(command.DisplayMode, ignoreCase: true, out var mode))
            return Fail(SceneSessionActionIds.SetDisplayMode, "displayMode required.", "badMode");
        _document.Edit.DisplayMode = mode;
        RaiseChanged("setdisplaymode");
        return Ok(SceneSessionActionIds.SetDisplayMode, $"Display {mode}.");
    }

    private AgentCommandResult DoMakeEditable(AgentCommand command)
    {
        var id = ResolveEditTarget(command.NodeId);
        if (id is null)
            return Fail(SceneSessionActionIds.MakeEditable, "Select a mesh or generator.", "noSelection");
        if (!MeshEditBake.MakeEditable(_document, _evaluator, id.Value))
            return Fail(SceneSessionActionIds.MakeEditable, "Could not bake.", "badNode");
        RaiseChanged("makeeditable");
        return Ok(SceneSessionActionIds.MakeEditable, "Made editable.", _document.SelectionId?.ToString());
    }

    private AgentCommandResult DoSelectComponents(AgentCommand command)
    {
        var edit = _document.Edit;
        if (edit.Mode == SceneEditMode.Object)
            return Fail(SceneSessionActionIds.SelectComponents, "Not in component mode.", "badMode");

        var additive = command.Additive == true;
        if (!additive)
            edit.ClearComponents();

        if (string.IsNullOrWhiteSpace(command.Indices))
        {
            RaiseChanged("selectcomponents");
            return Ok(SceneSessionActionIds.SelectComponents, "Cleared components.");
        }

        if (edit.EditMeshId is null && _document.SelectionId is { } sel)
            edit.EditMeshId = sel;

        foreach (var token in command.Indices.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (edit.Mode == SceneEditMode.Edge && token.Contains('-', StringComparison.Ordinal))
            {
                var parts = token.Split('-', 2);
                if (int.TryParse(parts[0], out var a) && int.TryParse(parts[1], out var b))
                {
                    var key = a < b ? (a, b) : (b, a);
                    edit.SelectedEdges.Add(key);
                }
            }
            else if (int.TryParse(token, out var idx))
            {
                if (edit.Mode == SceneEditMode.Point)
                    edit.SelectedVertices.Add(idx);
                else if (edit.Mode == SceneEditMode.Polygon)
                    edit.SelectedFaces.Add(idx);
            }
        }

        RaiseChanged("selectcomponents");
        return Ok(SceneSessionActionIds.SelectComponents, $"Selected {edit.SelectionCount} components.");
    }

    private AgentCommandResult DoMoveSelection(AgentCommand command)
    {
        var delta = new System.Numerics.Vector3(command.X ?? 0, command.Y ?? 0, command.Z ?? 0);
        if (delta == System.Numerics.Vector3.Zero)
            return Ok(SceneSessionActionIds.MoveSelection, "No delta.");

        var edit = _document.Edit;
        if (edit.Mode == SceneEditMode.Object)
        {
            if (!TryGetSelectedOrNode(command.NodeId, out var node) || node is null)
                return Fail(SceneSessionActionIds.MoveSelection, "Select a node.", "noSelection");
            node.Transform.Position[0] += delta.X;
            node.Transform.Position[1] += delta.Y;
            node.Transform.Position[2] += delta.Z;
            _evaluator.NotifyNodeChanged(node);
            RaiseChanged("moveselection");
            return Ok(SceneSessionActionIds.MoveSelection, "Moved object.", node.Id.ToString());
        }

        if (!TryGetEditableMesh(out var meshNode, out var mesh))
            return Fail(SceneSessionActionIds.MoveSelection, "No editable mesh.", "noMesh");

        var updated = edit.Mode switch
        {
            SceneEditMode.Point => MeshComponentOps.MoveVertices(mesh, edit.SelectedVertices, delta),
            SceneEditMode.Edge => MeshComponentOps.MoveEdges(mesh, edit.SelectedEdges, delta),
            SceneEditMode.Polygon => MeshComponentOps.MoveFaces(mesh, edit.SelectedFaces, delta),
            _ => mesh,
        };
        MeshEditBake.WriteBaked(meshNode, updated);
        _evaluator.NotifyNodeChanged(meshNode);
        RaiseChanged("moveselection");
        return Ok(SceneSessionActionIds.MoveSelection, "Moved selection.", meshNode.Id.ToString());
    }

    private AgentCommandResult DoMeshEdit(AgentCommand command)
    {
        if (!Enum.TryParse<ModifierKind>(command.ModifierKind, ignoreCase: true, out var kind))
            return Fail(SceneSessionActionIds.MeshEdit, "modifierKind required.", "badKind");

        var edit = _document.Edit;
        if (edit.Mode == SceneEditMode.Object)
        {
            return DoAddModifier(command);
        }

        if (!TryGetEditableMesh(out var meshNode, out var mesh))
            return Fail(SceneSessionActionIds.MeshEdit, "Make editable first.", "noMesh");

        var distance = command.Distance ?? 0.2f;
        EditableMesh updated = kind switch
        {
            ModifierKind.Extrude when edit.Mode == SceneEditMode.Polygon && edit.SelectedFaces.Count > 0 =>
                MeshComponentOps.ExtrudeFaces(mesh, edit.SelectedFaces, distance),
            ModifierKind.Inset when edit.Mode == SceneEditMode.Polygon && edit.SelectedFaces.Count > 0 =>
                MeshComponentOps.InsetFaces(mesh, edit.SelectedFaces, distance),
            ModifierKind.Bevel when edit.Mode == SceneEditMode.Edge && edit.SelectedEdges.Count > 0 =>
                MeshComponentOps.BevelEdges(mesh, edit.SelectedEdges, distance),
            ModifierKind.Dissolve when edit.Mode == SceneEditMode.Polygon =>
                MeshComponentOps.DissolveFaces(mesh, edit.SelectedFaces),
            ModifierKind.Dissolve when edit.Mode == SceneEditMode.Edge =>
                MeshComponentOps.DissolveEdges(mesh, edit.SelectedEdges),
            ModifierKind.Knife =>
                MeshComponentOps.Knife(mesh, new System.Numerics.Plane(System.Numerics.Vector3.UnitY, 0)),
            ModifierKind.Bridge =>
                MeshComponentOps.BridgeSelectedEdges(mesh, edit.SelectedEdges.ToList()),
            ModifierKind.Weld =>
                MeshWeld.Apply(mesh, new WeldOptions(
                    command.Distance ?? 0.001f,
                    Scope: edit.SelectedVertices.Count > 0
                        ? WeldScope.SelectedVertices
                        : WeldScope.EntireMesh),
                    edit.SelectedVertices.Count > 0 ? edit.SelectedVertices : null),
            ModifierKind.Optimize => MeshOptimize.Apply(mesh).Mesh,
            ModifierKind.Subdivision => MeshShaping.Subdivide(mesh, command.Count ?? 1),
            ModifierKind.Extrude => MeshShaping.Extrude(mesh, distance),
            ModifierKind.Bevel => MeshShaping.BevelLite(mesh, distance),
            _ => mesh.Clone(),
        };

        MeshEditBake.WriteBaked(meshNode, updated);
        edit.ClearComponents();
        _evaluator.NotifyNodeChanged(meshNode);
        RaiseChanged("meshedit");
        return Ok(SceneSessionActionIds.MeshEdit, $"Applied {kind}.", meshNode.Id.ToString());
    }

    private Guid? ResolveEditTarget(string? nodeId)
    {
        if (!string.IsNullOrWhiteSpace(nodeId) && Guid.TryParse(nodeId, out var id))
            return id;
        if (_document.Edit.EditMeshId is { } eid)
            return eid;
        return _document.SelectionId;
    }

    private bool TryGetEditableMesh(out MeshNode meshNode, out EditableMesh mesh)
    {
        meshNode = null!;
        mesh = null!;
        var id = _document.Edit.EditMeshId ?? _document.SelectionId;
        if (id is null || _document.Find(id.Value) is not MeshNode node)
            return false;
        if (node.Vertices is null || node.Indices is null)
        {
            if (!MeshEditBake.MakeEditable(_document, _evaluator, node.Id))
                return false;
        }

        meshNode = node;
        mesh = MeshEditBake.ReadBakedOrTessellate(node);
        return true;
    }

    private Guid? ResolveParent(string? parentId)
    {
        if (!string.IsNullOrWhiteSpace(parentId) && Guid.TryParse(parentId, out var pid))
            return pid;
        return _document.Roots().FirstOrDefault()?.Id;
    }

    private bool TryGetSelectedOrNode(string? nodeId, out SceneNode? node)
    {
        node = null;
        if (!string.IsNullOrWhiteSpace(nodeId) && Guid.TryParse(nodeId, out var id))
        {
            node = _document.Find(id);
            return node is not null;
        }

        if (_document.SelectionId is { } sel)
        {
            node = _document.Find(sel);
            return node is not null;
        }

        return false;
    }

    private static LightKind ParseLightKind(string? raw) =>
        Enum.TryParse<LightKind>(raw, ignoreCase: true, out var kind) ? kind : LightKind.Omni;

    private static string NodeKindLabel(SceneNode n) => n switch
    {
        GroupNode => "group",
        MeshNode => "mesh",
        GeneratorNode => "generator",
        ModifierNode => "modifier",
        MaterialNode => "material",
        LightNode => "light",
        CameraNode => "camera",
        NullNode => "null",
        _ => "node",
    };

    private static int RankPhrase(string phrase, string name, string id)
    {
        if (string.Equals(name, phrase, StringComparison.OrdinalIgnoreCase))
            return 100;
        if (string.Equals(id, phrase, StringComparison.OrdinalIgnoreCase))
            return 95;
        if (name.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            return 70;
        if (id.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            return 60;
        return 0;
    }

    private static bool TryResolveTriangleSoup(
        AgentCommand command,
        out float[] vertices,
        out int[] indices,
        out string? nameHint,
        out string error)
    {
        vertices = [];
        indices = [];
        nameHint = null;
        error = "";

        float[]? verts = null;
        int[]? inds = null;

        if (!string.IsNullOrWhiteSpace(command.Path))
        {
            if (!File.Exists(command.Path))
            {
                error = "file not found.";
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(command.Path));
                var root = doc.RootElement;
                verts = ReadFloatArray(root, "vertices");
                inds = ReadIntArray(root, "indices");
                if (root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                    nameHint = nameEl.GetString();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        verts ??= ParseFloatList(command.Vertices ?? command.Get("vertices"));
        inds ??= ParseIntList(command.Indices ?? command.Get("indices"));

        if (verts is null || verts.Length < 9 || verts.Length % 3 != 0)
        {
            error = "vertices required (xyz triples, at least 3).";
            return false;
        }

        if (inds is null || inds.Length < 3 || inds.Length % 3 != 0)
        {
            error = "indices required (triangle triples).";
            return false;
        }

        var max = verts.Length / 3;
        foreach (var i in inds)
        {
            if (i < 0 || i >= max)
            {
                error = $"index {i} out of range (0..{max - 1}).";
                return false;
            }
        }

        vertices = verts;
        indices = inds;
        return true;
    }

    private static float[]? ParseFloatList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        raw = raw.Trim();
        if (raw.StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<float[]>(raw);
            }
            catch
            {
                return null;
            }
        }

        var parts = raw.Split([',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var list = new float[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out list[i]))
                return null;
        }

        return list;
    }

    private static int[]? ParseIntList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        raw = raw.Trim();
        if (raw.StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<int[]>(raw);
            }
            catch
            {
                return null;
            }
        }

        var parts = raw.Split([',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var list = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out list[i]))
                return null;
        }

        return list;
    }

    private static float[]? ReadFloatArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
            return null;
        var list = new float[el.GetArrayLength()];
        var i = 0;
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind is not (JsonValueKind.Number))
                return null;
            list[i++] = item.GetSingle();
        }

        return list;
    }

    private static int[]? ReadIntArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
            return null;
        var list = new int[el.GetArrayLength()];
        var i = 0;
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind is not JsonValueKind.Number)
                return null;
            list[i++] = item.GetInt32();
        }

        return list;
    }

    private static void FrameEditableMesh(EditableMesh editable, float? targetLengthMeters)
    {
        if (editable.VertexCount == 0)
            return;

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < editable.VertexCount; i++)
        {
            var v = editable.Vertices[i];
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        var size = max - min;
        var longest = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        if (longest < 1e-6f)
            return;

        var center = (min + max) * 0.5f;
        editable.Transform(Matrix4x4.CreateTranslation(-center));
        if (targetLengthMeters is > 0f)
            editable.Transform(Matrix4x4.CreateScale(targetLengthMeters.Value / longest));
    }

    private void RaiseChanged(string reason)
    {
        void Raise()
        {
            Revision++;
            DocumentChanged?.Invoke();
            if (_subscribed)
            {
                Changed?.Invoke(new AgentChangedEvent
                {
                    Reason = reason,
                    DocumentName = _document.Name,
                    NodeCount = _document.Nodes.Count,
                });
            }
        }

        if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            Raise();
        else
            global::Avalonia.Threading.Dispatcher.UIThread.Post(Raise);
    }

    private static AgentCommandResult Ok(string actionId, string message, string? nodeId = null) => new()
    {
        Ok = true,
        ActionId = actionId,
        Message = message,
        NodeId = nodeId,
    };

    private static AgentCommandResult Fail(string actionId, string message, string code) => new()
    {
        Ok = false,
        ActionId = actionId,
        Message = message,
        ErrorCode = code,
    };
}
