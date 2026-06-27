using System;
using System.Numerics;

namespace Engine;


public struct OBB {
    public Vector3 Center;
    public Vector3 AxisX;
    public Vector3 AxisY;
    public Vector3 AxisZ;
    public Vector3 HalfExtents;

    /// Returns the 8 world-space corners.
    public void GetCorners (Span<Vector3> out8) {
        Vector3 px = AxisX * HalfExtents.X, py = AxisY * HalfExtents.Y, pz = AxisZ * HalfExtents.Z;
        out8[0] = Center + px + py + pz; out8[1] = Center - px + py + pz;
        out8[2] = Center + px - py + pz; out8[3] = Center - px - py + pz;
        out8[4] = Center + px + py - pz; out8[5] = Center - px + py - pz;
        out8[6] = Center + px - py - pz; out8[7] = Center - px - py - pz;
    }
}
