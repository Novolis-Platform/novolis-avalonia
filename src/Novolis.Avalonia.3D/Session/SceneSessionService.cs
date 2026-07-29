using Novolis.Agent.Surface;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Session;

/// <summary>Scene mutations for UI and agent transports.</summary>
public sealed class SceneSessionService : ISceneSession
{
    private readonly SceneEvaluator _evaluator = new();
    private SceneDocument _document;
    private string? _path;
    private string? _lastAction;
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

    public event Action? DocumentChanged;
    public event Action<AgentChangedEventDto>? Changed;
    public event Action<AgentActionResultEventDto>? ActionResult;

    public AgentHelloDto Hello() => Definition.BuildHello(AppId);

    public AgentSnapshotDto Snapshot() => new()
    {
        DocumentName = _document.Name,
        NodeCount = _document.Nodes.Count,
        SelectionId = _document.SelectionId?.ToString(),
        ActiveCameraId = _document.ActiveCameraId?.ToString(),
        LastAction = _lastAction,
        Document = _document,
        Actions = Actions().Actions,
    };

    public AgentActionsResponseDto Actions() => Definition.BuildActions(a =>
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

    public AgentCommandResultDto Execute(AgentCommandDto command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = (command.ActionId ?? "").Trim().ToLowerInvariant();
        AgentCommandResultDto result;
        try
        {
            result = id switch
            {
                SceneSessionActionIds.New => DoNew(),
                SceneSessionActionIds.Open => DoOpen(command),
                SceneSessionActionIds.Save => DoSave(command),
                SceneSessionActionIds.Select => DoSelect(command),
                SceneSessionActionIds.Delete => DoDelete(),
                SceneSessionActionIds.Fit => Ok(id, "Fit."),
                SceneSessionActionIds.AddLight => DoAddLight(command),
                SceneSessionActionIds.AddCamera => DoAddCamera(command),
                SceneSessionActionIds.AddMesh => DoAddMesh(command),
                SceneSessionActionIds.AddMaterial => DoAddMaterial(command),
                SceneSessionActionIds.AddGenerator => DoAddGenerator(command),
                SceneSessionActionIds.AddModifier => DoAddModifier(command),
                SceneSessionActionIds.SetLight => DoSetLight(command),
                SceneSessionActionIds.SetTransform => DoSetTransform(command),
                SceneSessionActionIds.SetActiveCamera => DoSetActiveCamera(command),
                _ => Fail(id, $"Unknown action '{command.ActionId}'.", "unknownAction"),
            };
        }
        catch (Exception ex)
        {
            result = Fail(id, ex.Message, "exception");
        }

        _lastAction = result.ActionId;
        if (_subscribed)
            ActionResult?.Invoke(new AgentActionResultEventDto
            {
                Ok = result.Ok,
                ActionId = result.ActionId,
                Message = result.Message,
            });
        return result;
    }

    public void Subscribe() => _subscribed = true;

    public void ReplaceDocument(SceneDocument document, string? path = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _path = path;
        _evaluator.Bind(_document);
        RaiseChanged("replace");
    }

    private AgentCommandResultDto DoNew()
    {
        ReplaceDocument(SceneDocument.CreateEmpty());
        return Ok(SceneSessionActionIds.New, "New scene.");
    }

    private AgentCommandResultDto DoOpen(AgentCommandDto command)
    {
        if (string.IsNullOrWhiteSpace(command.Path))
            return Fail(SceneSessionActionIds.Open, "path required.", "badPath");
        var doc = SceneSerializer.Load(command.Path);
        ReplaceDocument(doc, command.Path);
        return Ok(SceneSessionActionIds.Open, $"Opened {Path.GetFileName(command.Path)}.");
    }

    private AgentCommandResultDto DoSave(AgentCommandDto command)
    {
        var path = command.Path ?? _path;
        if (string.IsNullOrWhiteSpace(path))
            return Fail(SceneSessionActionIds.Save, "path required.", "badPath");
        SceneSerializer.Save(_document, path);
        _path = path;
        return Ok(SceneSessionActionIds.Save, $"Saved {Path.GetFileName(path)}.");
    }

    private AgentCommandResultDto DoSelect(AgentCommandDto command)
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

    private AgentCommandResultDto DoDelete()
    {
        if (_document.SelectionId is not { } id)
            return Fail(SceneSessionActionIds.Delete, "Nothing selected.", "noSelection");
        if (!_document.TryRemove(id))
            return Fail(SceneSessionActionIds.Delete, "Not found.", "badNode");
        _evaluator.InvalidateAll();
        RaiseChanged("delete");
        return Ok(SceneSessionActionIds.Delete, "Deleted.");
    }

    private AgentCommandResultDto DoAddLight(AgentCommandDto command)
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

    private AgentCommandResultDto DoAddCamera(AgentCommandDto command)
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

    private AgentCommandResultDto DoAddMesh(AgentCommandDto command)
    {
        var mesh = new MeshNode
        {
            Name = string.IsNullOrWhiteSpace(command.Name) ? "Mesh" : command.Name!,
            ParentId = ResolveParent(command.ParentId),
            Transform = new SceneTransform
            {
                Position = [command.X ?? 0f, command.Y ?? 0.5f, command.Z ?? 0f],
            },
        };
        _document.Nodes.Add(mesh);
        _document.SelectionId = mesh.Id;
        _evaluator.NotifyNodeChanged(mesh);
        RaiseChanged("addmesh");
        return Ok(SceneSessionActionIds.AddMesh, "Added mesh.", mesh.Id.ToString());
    }

    private AgentCommandResultDto DoAddMaterial(AgentCommandDto command)
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

    private AgentCommandResultDto DoAddGenerator(AgentCommandDto command)
    {
        var kind = Enum.TryParse<GeneratorKind>(command.GeneratorKind, ignoreCase: true, out var g)
            ? g
            : GeneratorKind.Cloner;
        Guid? sourceId = null;
        if (!string.IsNullOrWhiteSpace(command.SourceId) && Guid.TryParse(command.SourceId, out var sid))
            sourceId = sid;
        else if (_document.SelectionId is { } sel && _document.Find(sel) is MeshNode)
            sourceId = sel;

        var gen = new GeneratorNode
        {
            Name = kind.ToString(),
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

    private AgentCommandResultDto DoAddModifier(AgentCommandDto command)
    {
        var kind = Enum.TryParse<ModifierKind>(command.ModifierKind, ignoreCase: true, out var m)
            ? m
            : ModifierKind.Weld;
        Guid? inputId = null;
        if (!string.IsNullOrWhiteSpace(command.InputId) && Guid.TryParse(command.InputId, out var iid))
            inputId = iid;
        else if (_document.SelectionId is { } sel)
            inputId = sel;

        var mod = new ModifierNode
        {
            Name = kind.ToString(),
            ParentId = ResolveParent(command.ParentId),
            Modifier = kind,
            InputId = inputId,
        };
        _document.Nodes.Add(mod);
        _document.SelectionId = mod.Id;
        _evaluator.InvalidateMesh();
        RaiseChanged("addmodifier");
        return Ok(SceneSessionActionIds.AddModifier, $"Added {kind}.", mod.Id.ToString());
    }

    private AgentCommandResultDto DoSetLight(AgentCommandDto command)
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

    private AgentCommandResultDto DoSetTransform(AgentCommandDto command)
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

    private AgentCommandResultDto DoSetActiveCamera(AgentCommandDto command)
    {
        if (!Guid.TryParse(command.NodeId, out var id) || _document.Find(id) is not CameraNode)
            return Fail(SceneSessionActionIds.SetActiveCamera, "Camera node required.", "badNode");
        _document.ActiveCameraId = id;
        RaiseChanged("setactivecamera");
        return Ok(SceneSessionActionIds.SetActiveCamera, "Active camera set.", id.ToString());
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

    private void RaiseChanged(string reason)
    {
        DocumentChanged?.Invoke();
        if (_subscribed)
        {
            Changed?.Invoke(new AgentChangedEventDto
            {
                Reason = reason,
                DocumentName = _document.Name,
                NodeCount = _document.Nodes.Count,
            });
        }
    }

    private static AgentCommandResultDto Ok(string actionId, string message, string? nodeId = null) => new()
    {
        Ok = true,
        ActionId = actionId,
        Message = message,
        NodeId = nodeId,
    };

    private static AgentCommandResultDto Fail(string actionId, string message, string code) => new()
    {
        Ok = false,
        ActionId = actionId,
        Message = message,
        ErrorCode = code,
    };
}
