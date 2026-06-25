using System;
using System.Numerics;

namespace Engine;


public static class PhysicsManager {

    private const float CellSize = 4f;

    private static readonly List<ColliderComponent> dynamicColliders = new();
    private static readonly List<ColliderComponent> staticColliders = new();
    private static readonly Dictionary<(int, int, int), List<ColliderComponent>> grid = new();

    private const float PositionCorrectionPercent = 0.2f; /// only correct part of the penetration per step
    private const float PenetrationSlop = 0.01f; /// ignore tiny penetration, don't fight floating point noise


    public static void Register (ColliderComponent collider) {
        if (collider.isStatic) staticColliders.Add(collider);
        else dynamicColliders.Add(collider);
    }
    public static void Unregister (ColliderComponent collider) {
        if (collider.isStatic) staticColliders.Remove(collider);
        else dynamicColliders.Remove(collider);
    }

    public static void FixedUpdate () {
        RebuildGrid();
        ResolveDynamicPairs();
        ResolveAgainstStatic();
    }

    private static void RebuildGrid () {
        grid.Clear();
        foreach (ColliderComponent c in dynamicColliders) {
            Bounds b = c.GetWorldBounds();
            (int, int, int) min = CellOf(b.Min);
            (int, int, int) max = CellOf(b.Max);

            for (int x = min.Item1; x <= max.Item1; x++) {
                for (int y = min.Item2; y <= max.Item2; y++) {
                    for (int z = min.Item3; z <= max.Item3; z++) {
                        (int, int, int) key = (x, y, z);
                        if (!grid.TryGetValue(key, out List<ColliderComponent>? bucket)) {
                            bucket = new List<ColliderComponent>();
                            grid[key] = bucket;
                        }
                        bucket.Add(c);
                    }
                }
            }
        }
    }

    private static void ResolveDynamicPairs () {
        HashSet<(int, int)> checkedPairs = new();

        foreach (List<ColliderComponent> bucket in grid.Values) {
            for (int i = 0; i < bucket.Count; i++) {
                for (int j = i + 1; j < bucket.Count; j++) {
                    ColliderComponent a = bucket[i];
                    ColliderComponent b = bucket[j];

                    int idA = a.owner.Id;
                    int idB = b.owner.Id;
                    (int, int) pairKey = idA < idB ? (idA, idB) : (idB, idA);
                    if (!checkedPairs.Add(pairKey)) continue;

                    if (!a.GetWorldBounds().Intersects(b.GetWorldBounds())) continue;
                    Log.log("bounds intersect, checking narrow phase");
                    if (a.Overlaps(b, out Contact contact)) Resolve(a, b, contact);
                }
            }
        }
    }

    private static void ResolveAgainstStatic () {
        foreach (ColliderComponent dyn in dynamicColliders) {
            Bounds dynBounds = dyn.GetWorldBounds();
            foreach (ColliderComponent stat in staticColliders) {
                if (!dynBounds.Intersects(stat.GetWorldBounds())) continue;
                if (dyn.Overlaps(stat, out Contact contact)) Resolve(dyn, stat, contact);
            }
        }
    }

    private static (int, int, int) CellOf (Vector3 p) {
        return (
            (int)MathF.Floor(p.X/CellSize),
            (int)MathF.Floor(p.Y/CellSize),
            (int)MathF.Floor(p.Z/CellSize)
        );
    }

    private static void Resolve (ColliderComponent a, ColliderComponent b, Contact contact) {
        PhysicsComponent physA = a.owner.GetComponent<PhysicsComponent>();
        PhysicsComponent physB = b.owner.GetComponent<PhysicsComponent>();

        bool aDynamic = physA != null && !physA.isKinematic;
        bool bDynamic = physB != null && !physB.isKinematic;
        if (!aDynamic && !bDynamic) return;

        float pushA = aDynamic && bDynamic ? 0.5f : aDynamic ? 1f : 0f;
        float pushB = aDynamic && bDynamic ? 0.5f : bDynamic ? 1f : 0f;

        float correction = MathF.Max(contact.penetration - PenetrationSlop, 0f)*PositionCorrectionPercent;

        if (aDynamic) a.owner.Transform.Position -= pushA*correction*contact.normal;
        if (bDynamic) b.owner.Transform.Position += pushB*correction*contact.normal;

        float relativeVelocity = Vector3.Dot(physA?.Velocity ?? Vector3.Zero, contact.normal)
                                - Vector3.Dot(physB?.Velocity ?? Vector3.Zero, contact.normal);

        /// only kill velocity if objects are moving INTO each other, not already separating
        if (relativeVelocity < 0f) {
            if (aDynamic) physA.AddImpulse(-relativeVelocity*pushA*2f*contact.normal);
            if (bDynamic) physB.AddImpulse(relativeVelocity*pushB*2f*contact.normal);
        }
    }


}
