using System;
using System.Numerics;

namespace Engine;

public static class OBBCollision {

    // ── public entry point ───────────────────────────────────────────────────

    public static Manifold Test (in OBB a, in OBB b) {
        // Rotation matrix expressing b's axes in a's local frame.
        // R[i,j] = dot(a.Axis_i, b.Axis_j)
        Span<float> R = stackalloc float[9];
        Span<float> AR = stackalloc float[9];    // abs(R) + epsilon for edge-edge
        const float eps = 1e-4f;

        R[0] = Vector3.Dot(a.AxisX, b.AxisX); R[1] = Vector3.Dot(a.AxisX, b.AxisY); R[2] = Vector3.Dot(a.AxisX, b.AxisZ);
        R[3] = Vector3.Dot(a.AxisY, b.AxisX); R[4] = Vector3.Dot(a.AxisY, b.AxisY); R[5] = Vector3.Dot(a.AxisY, b.AxisZ);
        R[6] = Vector3.Dot(a.AxisZ, b.AxisX); R[7] = Vector3.Dot(a.AxisZ, b.AxisY); R[8] = Vector3.Dot(a.AxisZ, b.AxisZ);
        for (int i = 0; i < 9; i++) AR[i] = MathF.Abs(R[i]) + eps;

        Vector3 t = b.Center - a.Center;
        // Express translation in a's frame
        float tx = Vector3.Dot(t, a.AxisX), ty = Vector3.Dot(t, a.AxisY), tz = Vector3.Dot(t, a.AxisZ);

        float minPen = float.MaxValue;
        int bestAxis = -1;
        bool bestFromA = false;

        // ── 6 face axes ──────────────────────────────────────────────────────
        // A face axes
        if (!FaceTest(MathF.Abs(tx), a.HalfExtents.X, b.HalfExtents.X*AR[0]+b.HalfExtents.Y*AR[1]+b.HalfExtents.Z*AR[2], ref minPen, ref bestAxis, ref bestFromA, 0, true)) return default;
        if (!FaceTest(MathF.Abs(ty), a.HalfExtents.Y, b.HalfExtents.X*AR[3]+b.HalfExtents.Y*AR[4]+b.HalfExtents.Z*AR[5], ref minPen, ref bestAxis, ref bestFromA, 1, true)) return default;
        if (!FaceTest(MathF.Abs(tz), a.HalfExtents.Z, b.HalfExtents.X*AR[6]+b.HalfExtents.Y*AR[7]+b.HalfExtents.Z*AR[8], ref minPen, ref bestAxis, ref bestFromA, 2, true)) return default;

        // B face axes (project t onto b's local frame)
        float bx = Vector3.Dot(t, b.AxisX), by = Vector3.Dot(t, b.AxisY), bz = Vector3.Dot(t, b.AxisZ);
        if (!FaceTest(MathF.Abs(bx), b.HalfExtents.X, a.HalfExtents.X*AR[0]+a.HalfExtents.Y*AR[3]+a.HalfExtents.Z*AR[6], ref minPen, ref bestAxis, ref bestFromA, 0, false)) return default;
        if (!FaceTest(MathF.Abs(by), b.HalfExtents.Y, a.HalfExtents.X*AR[1]+a.HalfExtents.Y*AR[4]+a.HalfExtents.Z*AR[7], ref minPen, ref bestAxis, ref bestFromA, 1, false)) return default;
        if (!FaceTest(MathF.Abs(bz), b.HalfExtents.Z, a.HalfExtents.X*AR[2]+a.HalfExtents.Y*AR[5]+a.HalfExtents.Z*AR[8], ref minPen, ref bestAxis, ref bestFromA, 2, false)) return default;

        // ── 9 edge-cross axes ────────────────────────────────────────────────
        // AX × BX, AX × BY, … (see Ericson "Real-Time Collision Detection" §4.4)
        Span<Vector3> aAxes = stackalloc Vector3[3] { a.AxisX, a.AxisY, a.AxisZ };
        Span<Vector3> bAxes = stackalloc Vector3[3] { b.AxisX, b.AxisY, b.AxisZ };

        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                Vector3 axis = Vector3.Cross(aAxes[i], bAxes[j]);
                float len = axis.Length();
                if (len < 1e-5f) continue;   // parallel edges — skip
                axis /= len;

                float proj_a = EdgeProject(a, axis);
                float proj_b = EdgeProject(b, axis);
                float dist = MathF.Abs(Vector3.Dot(t, axis));
                float pen = proj_a + proj_b - dist;
                if (pen < 0f) return default;

                // edge-edge axes get a slight bias so face contacts win tiebreaks
                float biasedPen = pen - 0.005f;
                if (biasedPen < minPen) {
                    minPen   = biasedPen;
                    bestAxis = i * 3 + j + 6;   // 6–14
                    bestFromA = true;   // (unused for edge; we store the actual axis below)
                }
            }
        }

        // ── build manifold ───────────────────────────────────────────────────
        return BuildManifold(a, b, minPen, bestAxis, bestFromA, t, aAxes, bAxes, R);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    static bool FaceTest (float dist, float halfA, float halfB,
                          ref float minPen, ref int bestAxis, ref bool bestFromA,
                          int axis, bool fromA) {
        float pen = halfA + halfB - dist;
        if (pen < 0f) return false;
        if (pen < minPen) { minPen = pen; bestAxis = axis; bestFromA = fromA; }
        return true;
    }

    static float EdgeProject (in OBB o, Vector3 axis) =>
        o.HalfExtents.X * MathF.Abs(Vector3.Dot(o.AxisX, axis)) +
        o.HalfExtents.Y * MathF.Abs(Vector3.Dot(o.AxisY, axis)) +
        o.HalfExtents.Z * MathF.Abs(Vector3.Dot(o.AxisZ, axis));

    // ── manifold builder ─────────────────────────────────────────────────────

    static Manifold BuildManifold (in OBB a, in OBB b, float pen, int axisIdx, bool fromA,
                                   Vector3 t,
                                   Span<Vector3> aAxes, Span<Vector3> bAxes,
                                   Span<float> R) {
        Manifold m = default;
        m.Colliding = true;

        // ── determine contact normal ─────────────────────────────────────────
        Vector3 normal;
        if (axisIdx < 3) {
            // A face axis
            normal = aAxes[axisIdx];
            if (Vector3.Dot(normal, t) < 0f) normal = -normal;
        } else if (axisIdx < 6) {
            // B face axis
            normal = bAxes[axisIdx - 3];
            if (Vector3.Dot(normal, t) < 0f) normal = -normal;
        } else {
            // Edge-edge: recompute
            int i = (axisIdx - 6) / 3, j = (axisIdx - 6) % 3;
            normal = Vector3.Normalize(Vector3.Cross(aAxes[i], bAxes[j]));
            if (Vector3.Dot(normal, t) < 0f) normal = -normal;
        }
        m.Normal = normal;

        // ── for edge-edge: single mid-point contact ──────────────────────────
        if (axisIdx >= 6) {
            Vector3 pa = a.Center, pb = b.Center;
            m.Add(0.5f * (pa + pb), pen);
            return m;
        }

        // ── face contact: clip incident face against reference face ──────────
        // Reference OBB and incident OBB
        bool refIsA = fromA;
        OBB refOBB = refIsA ? a : b;
        OBB incOBB = refIsA ? b : a;

        // Reference face: axis of refOBB most aligned with normal
        int refFaceIdx = MostAlignedFace(refOBB, normal);
        Vector3 refFaceNormal = FaceAxis(refOBB, refFaceIdx, normal);

        // Incident face: axis of incOBB most anti-parallel to normal
        int incFaceIdx = MostAlignedFace(incOBB, -normal);
        Vector3 incFaceNormal = FaceAxis(incOBB, incFaceIdx, -normal);

        // Reference face center
        Vector3 refFaceCenter = refOBB.Center + refFaceNormal * HalfExtentForFace(refOBB, refFaceIdx);

        // Incident face — 4 vertices
        Span<Vector3> incVerts = stackalloc Vector3[8];
        incOBB.GetCorners(incVerts);

        // Choose the 4 corners on the incident face (positive dot with incFaceNormal)
        Span<Vector3> faceVerts = stackalloc Vector3[4];
        int fv = 0;
        for (int i = 0; i < 8 && fv < 4; i++) {
            if (Vector3.Dot(incVerts[i] - incOBB.Center, incFaceNormal) > 0f)
                faceVerts[fv++] = incVerts[i];
        }

        // Clip against the 4 side planes of the reference face
        Span<Vector3> clip = stackalloc Vector3[8];
        Span<Vector3> tmp = stackalloc Vector3[8];
        faceVerts[..4].CopyTo(clip);
        int clipCount = 4;

        // Side planes: two tangent axes of the reference face
        GetFaceTangents(refOBB, refFaceIdx, out Vector3 tan0, out Vector3 tan1);
        float he0 = HalfExtentForTan(refOBB, refFaceIdx, 0);
        float he1 = HalfExtentForTan(refOBB, refFaceIdx, 1);

        clipCount = ClipByPlane(clip, clipCount, tmp, tan0, he0, refFaceCenter);
        clipCount = ClipByPlane(clip, clipCount, tmp, -tan0, he0, refFaceCenter);
        clipCount = ClipByPlane(clip, clipCount, tmp, tan1, he1, refFaceCenter);
        clipCount = ClipByPlane(clip, clipCount, tmp, -tan1, he1, refFaceCenter);

        // Keep only points below the reference plane (behind refFaceNormal)
        float refD = Vector3.Dot(refFaceNormal, refFaceCenter);
        const float baumgarte = 0.01f;  // tiny bias to quiet jitter at rest

        for (int i = 0; i < clipCount; i++) {
            float depth = refD - Vector3.Dot(refFaceNormal, clip[i]);
            if (depth >= -baumgarte)
                m.Add(clip[i], MathF.Max(depth, 0f));
            if (m.ContactCount == 4) break;
        }

        return m;
    }

    // ── face helpers ─────────────────────────────────────────────────────────

    static int MostAlignedFace (in OBB o, Vector3 dir) {
        float d0 = MathF.Abs(Vector3.Dot(o.AxisX, dir));
        float d1 = MathF.Abs(Vector3.Dot(o.AxisY, dir));
        float d2 = MathF.Abs(Vector3.Dot(o.AxisZ, dir));
        return d0 >= d1 && d0 >= d2 ? 0 : d1 >= d2 ? 1 : 2;
    }

    // Returns the face axis pointing in the same direction as hint.
    static Vector3 FaceAxis (in OBB o, int faceIdx, Vector3 hint) {
        Vector3 ax = faceIdx == 0 ? o.AxisX : faceIdx == 1 ? o.AxisY : o.AxisZ;
        return Vector3.Dot(ax, hint) >= 0f ? ax : -ax;
    }

    static float HalfExtentForFace (in OBB o, int idx) =>
        idx == 0 ? o.HalfExtents.X : idx == 1 ? o.HalfExtents.Y : o.HalfExtents.Z;

    static void GetFaceTangents (in OBB o, int faceIdx, out Vector3 tan0, out Vector3 tan1) {
        switch (faceIdx) {
            case 0: tan0 = o.AxisY; tan1 = o.AxisZ; break;
            case 1: tan0 = o.AxisX; tan1 = o.AxisZ; break;
            default: tan0 = o.AxisX; tan1 = o.AxisY; break;
        }
    }

    static float HalfExtentForTan (in OBB o, int faceIdx, int tanIdx) {
        return faceIdx switch {
            0 => tanIdx == 0 ? o.HalfExtents.Y : o.HalfExtents.Z,
            1 => tanIdx == 0 ? o.HalfExtents.X : o.HalfExtents.Z,
            _ => tanIdx == 0 ? o.HalfExtents.X : o.HalfExtents.Y,
        };
    }

    // ── Sutherland–Hodgman clip against one half-space ────────────────────────
    // Clips the polygon in `src[0..count]` against the plane defined by:
    //   dot(planeNormal, p - planeCenter) <= halfExtent
    // Output goes into `dst`; returns the new vertex count.
    // All managed — no unsafe needed.
    static int ClipByPlane (Span<Vector3> src, int count, Span<Vector3> dst,
                             Vector3 planeNormal, float halfExtent, Vector3 planeCenter) {
        int outCount = 0;
        for (int i = 0; i < count; i++) {
            Vector3 cur = src[i];
            Vector3 next = src[(i + 1) % count];

            float dCur = Vector3.Dot(planeNormal, cur  - planeCenter) - halfExtent;
            float dNext = Vector3.Dot(planeNormal, next - planeCenter) - halfExtent;

            if (dCur <= 0f) {
                if (outCount < dst.Length) dst[outCount++] = cur;
            }
            if ((dCur < 0f) != (dNext < 0f)) {
                float t = dCur / (dCur - dNext);
                Vector3 intersect = cur + t * (next - cur);
                if (outCount < dst.Length) dst[outCount++] = intersect;
            }
        }
        // Copy back to src for the next clip pass
        for (int i = 0; i < outCount; i++) src[i] = dst[i];
        return outCount;
    }
}