namespace Novolis.Avalonia.Cad.Scene;

using Novolis.Cad.Primitives;

/// <summary>Projected scene-tree row over a <see cref="CadEntity"/>.</summary>
public sealed record CadSceneTreeNode(
    Guid Id,
    string Name,
    string Kind,
    CadSceneNodeCategory Category,
    Guid? ParentId,
    IReadOnlyList<CadSceneTreeNode> Children,
    string? Role = null);
