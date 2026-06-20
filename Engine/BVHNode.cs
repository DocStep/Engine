using Silk.NET.Maths;

namespace Engine;


internal class BVHNode {
    internal BoundingBox Bounds;
    internal BVHNode? Left, Right;
    internal int[]? TriangleIndices;

    private const int LeafThreshold = 8;

    internal static BVHNode Build (Vector3D<float>[] verts, uint[] indices) {
        int triCount = indices.Length/3;
        int[] allTris = new int[triCount];
        for (int i = 0; i < triCount; i++) allTris[i] = i;
        return BuildRecursive(verts, indices, allTris);
    }

    private static BVHNode BuildRecursive (Vector3D<float>[] verts, uint[] indices, int[] tris) {
        var node = new BVHNode();
        node.Bounds = ComputeBounds(verts, indices, tris);

        if (tris.Length <= LeafThreshold) {
            node.TriangleIndices = tris;
            return node;
        }

        // Split on longest axis at centroid midpoint
        Vector3D<float> extent = node.Bounds.Max - node.Bounds.Min;
        int axis = extent.X >= extent.Y && extent.X >= extent.Z ? 0
                 : extent.Y >= extent.Z ? 1 : 2;

        float mid = axis == 0 ? (node.Bounds.Min.X + node.Bounds.Max.X)*0.5f
                  : axis == 1 ? (node.Bounds.Min.Y + node.Bounds.Max.Y)*0.5f
                  : (node.Bounds.Min.Z + node.Bounds.Max.Z)*0.5f;

        var leftTris = new List<int>();
        var rightTris = new List<int>();

        foreach (int ti in tris) {
            Vector3D<float> centroid = TriangleCentroid(verts, indices, ti);
            float val = axis == 0 ? centroid.X : axis == 1 ? centroid.Y : centroid.Z;
            (val < mid ? leftTris : rightTris).Add(ti);
        }

        // Degenerate split — just make a leaf
        if (leftTris.Count == 0 || rightTris.Count == 0) {
            node.TriangleIndices = tris;
            return node;
        }

        node.Left  = BuildRecursive(verts, indices, leftTris.ToArray());
        node.Right = BuildRecursive(verts, indices, rightTris.ToArray());
        return node;
    }

    private static BoundingBox ComputeBounds (Vector3D<float>[] verts, uint[] indices, int[] tris) {
        var min = new Vector3D<float>(float.MaxValue);
        var max = new Vector3D<float>(float.MinValue);

        foreach (int ti in tris) {
            for (int j = 0; j < 3; j++) {
                Vector3D<float> v = verts[indices[ti*3 + j]];
                min = Vector3D.Min(min, v);
                max = Vector3D.Max(max, v);
            }
        }

        return new BoundingBox(min, max);
    }

    private static Vector3D<float> TriangleCentroid (Vector3D<float>[] verts, uint[] indices, int ti) {
        Vector3D<float> v0 = verts[indices[ti*3]];
        Vector3D<float> v1 = verts[indices[ti*3 + 1]];
        Vector3D<float> v2 = verts[indices[ti*3 + 2]];
        return (v0 + v1 + v2) / 3f;
    }

    // Returns closest hit T, or null
    internal float? Intersect (
        Vector3D<float> origin, Vector3D<float> dir,
        Vector3D<float>[] verts, uint[] indices,
        Matrix4X4<float> model) {
        if (!Raycaster.IntersectAABB(origin, dir, Bounds.Min, Bounds.Max).HasValue)
            return null;

        // Leaf: test all triangles
        if (TriangleIndices is not null) {
            float? best = null;
            foreach (int ti in TriangleIndices) {
                Vector3D<float> v0 = Vector3D.Transform(verts[indices[ti*3]], model);
                Vector3D<float> v1 = Vector3D.Transform(verts[indices[ti*3 + 1]], model);
                Vector3D<float> v2 = Vector3D.Transform(verts[indices[ti*3 + 2]], model);
                float? t = Raycaster.IntersectTriangle(origin, dir, v0, v1, v2);
                if (t.HasValue && (!best.HasValue || t.Value < best.Value))
                    best = t;
            }
            return best;
        }

        // Interior: recurse
        float? tLeft = Left?.Intersect(origin, dir, verts, indices, model);
        float? tRight = Right?.Intersect(origin, dir, verts, indices, model);

        if (tLeft.HasValue && tRight.HasValue)
            return MathF.Min(tLeft.Value, tRight.Value);
        return tLeft ?? tRight;
    }
}
