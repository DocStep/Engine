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

    /// Center/extents form: transforms the center once, then the new half-size is |M| applied to the
    /// old half-size. Gives the same tight world-space AABB as transforming all 8 corners.
    /// Row-vector convention (v*M): x' = x*M11 + y*M21 + z*M31 + M41
    public readonly AABB Transformed_Alt (Matrix4x4 worldMatrix) {
        Vector3 c = (Min + Max)*0.5f;
        Vector3 e = (Max - Min)*0.5f;

        Vector3 nc = new Vector3(
            c.X*worldMatrix.M11 + c.Y*worldMatrix.M21 + c.Z*worldMatrix.M31 + worldMatrix.M41,
            c.X*worldMatrix.M12 + c.Y*worldMatrix.M22 + c.Z*worldMatrix.M32 + worldMatrix.M42,
            c.X*worldMatrix.M13 + c.Y*worldMatrix.M23 + c.Z*worldMatrix.M33 + worldMatrix.M43);

        Vector3 ne = new Vector3(
            MathF.Abs(worldMatrix.M11)*e.X + MathF.Abs(worldMatrix.M21)*e.Y + MathF.Abs(worldMatrix.M31)*e.Z,
            MathF.Abs(worldMatrix.M12)*e.X + MathF.Abs(worldMatrix.M22)*e.Y + MathF.Abs(worldMatrix.M32)*e.Z,
            MathF.Abs(worldMatrix.M13)*e.X + MathF.Abs(worldMatrix.M23)*e.Y + MathF.Abs(worldMatrix.M33)*e.Z);

        return new AABB(nc - ne, nc + ne);
    }

}
