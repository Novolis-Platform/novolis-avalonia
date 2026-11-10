using System.Numerics;
using Novolis.Avalonia.Ship.Design.Grips;
using Novolis.Cad.Primitives;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Plan;

public sealed partial class ShipDeckPlanViewport
{
    private void ApplyGrip(ShipObjectId oid, ShipGrip grip, Vector3 world)
    {
        if (TryApplyBulkheadGrip(oid, grip, world))
            return;
        if (TryApplyCompartmentGrip(oid, grip, world))
            return;
        if (TryApplyPassageGrip(oid, grip, world))
            return;
        TryApplyOpeningSlideGrip(oid, grip, world);
    }

    private bool TryApplyBulkheadGrip(ShipObjectId oid, ShipGrip grip, Vector3 world)
    {
        var bh = _session.Design.Bulkheads.FirstOrDefault(b => b.Id.Value == oid.Value);
        if (bh is null || grip.Kind is not (ShipGripKind.Endpoint or ShipGripKind.Vertex))
            return false;

        TryMutatePathVertex(bh.Geometry, grip, world, path =>
            _session.Mutate(d => ShipDesignMutations.UpdateBulkheadPath(d, bh.Id, path)));
        return true;
    }

    private bool TryApplyCompartmentGrip(ShipObjectId oid, ShipGrip grip, Vector3 world)
    {
        var room = _session.Design.Compartments.FirstOrDefault(c => c.Id.Value == oid.Value);
        if (room is null || grip.Kind is not (ShipGripKind.Vertex or ShipGripKind.Endpoint))
            return false;

        TryMutatePathVertex(room.Geometry, grip, world, path =>
            _session.Mutate(d => ShipDesignMutations.UpdateCompartmentPolygon(d, room.Id, path)));
        return true;
    }

    private bool TryApplyPassageGrip(ShipObjectId oid, ShipGrip grip, Vector3 world)
    {
        var passage = _session.Design.Passages.FirstOrDefault(p => p.Id.Value == oid.Value);
        if (passage is null)
            return false;

        if (grip.Kind is ShipGripKind.Endpoint or ShipGripKind.Vertex)
        {
            TryMutatePathVertex(passage.Geometry, grip, world, path =>
                _session.Mutate(d => ShipDesignMutations.UpdatePassagePath(d, passage.Id, path)));
            return true;
        }

        if (grip.Kind == ShipGripKind.Width)
        {
            ApplyPassageWidthGrip(passage, world);
            return true;
        }

        return false;
    }

    private void ApplyPassageWidthGrip(PassageDesign passage, Vector3 world)
    {
        var path = ShipPlanPaths.ExtractPathXz(passage.Geometry);
        var t = ShipPlanPaths.NearestParameter(path, world.X, world.Z);
        var on = ShipPlanPaths.PointAlong(path, t);
        var dist = MathF.Sqrt((world.X - on[0]) * (world.X - on[0]) + (world.Z - on[1]) * (world.Z - on[1]));
        _session.Mutate(d => ShipDesignMutations.SetPassageWidth(d, passage.Id, System.Math.Max(0.6f, dist * 2f)));
    }

    private void TryApplyOpeningSlideGrip(ShipObjectId oid, ShipGrip grip, Vector3 world)
    {
        if (grip.Kind != ShipGripKind.Slide)
            return;

        var opening = _session.Design.Openings.FirstOrDefault(o => o.Id.Value == oid.Value);
        if (opening is null)
            return;

        var host = _session.Design.Bulkheads.FirstOrDefault(b => b.Id.Value == opening.HostId.Value);
        if (host is null)
            return;

        var path = ShipPlanPaths.ExtractPathXz(host.Geometry);
        var t = ShipPlanPaths.NearestParameter(path, world.X, world.Z);
        _session.Mutate(d => ShipDesignMutations.MoveOpeningAlongHost(d, opening.Id, t));
    }

    private void TryMutatePathVertex(CadDocument geometry, ShipGrip grip, Vector3 world, Action<List<float[]>> apply)
    {
        var path = ShipPlanPaths.ExtractPathXz(geometry);
        var idx = NearestIndex(path, grip.X, grip.Z);
        if (idx < 0)
            return;

        path[idx] = [world.X, world.Z];
        apply(path);
    }
}
