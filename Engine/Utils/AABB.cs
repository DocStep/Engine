namespace Engine;


public struct AABB {
    public Vector3 Min;
    public Vector3 Max;

    public AABB (Vector3 min, Vector3 max) {
        Min = min;
        Max = max;
    }

    public static AABB FromVertices (Graphics.Vertex[] verts) {
        Vector3 min = verts[0].Position;
        Vector3 max = verts[0].Position;

        for (int i = 1; i < verts.Length; i++) {
            Vector3 p = verts[i].Position;
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        return new AABB(min, max);
    }

    /// Transforms the 8 corners by worldMatrix and rebuilds a tight world-space AABB.
    public AABB Transformed (Matrix4x4 worldMatrix) {
        Span<Vector3> corners = stackalloc Vector3[8];
        corners[0] = new Vector3(Min.X, Min.Y, Min.Z);
        corners[1] = new Vector3(Max.X, Min.Y, Min.Z);
        corners[2] = new Vector3(Min.X, Max.Y, Min.Z);
        corners[3] = new Vector3(Max.X, Max.Y, Min.Z);
        corners[4] = new Vector3(Min.X, Min.Y, Max.Z);
        corners[5] = new Vector3(Max.X, Min.Y, Max.Z);
        corners[6] = new Vector3(Min.X, Max.Y, Max.Z);
        corners[7] = new Vector3(Max.X, Max.Y, Max.Z);

        Vector3 newMin = Vector3.Transform(corners[0], worldMatrix);
        Vector3 newMax = newMin;

        for (int i = 1; i < 8; i++) {
            Vector3 p = Vector3.Transform(corners[i], worldMatrix);
            newMin = Vector3.Min(newMin, p);
            newMax = Vector3.Max(newMax, p);
        }

        return new AABB(newMin, newMax);
    }
}
