using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public struct Frustum () {
    readonly Vector4[] _planes = new Vector4[4];

    /// Row-vector convention: planes come from columns of viewProj
    public void Extract (in Matrix4x4 m) {
        _planes[0] = new Vector4(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41); /// Left
        _planes[1] = new Vector4(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41); /// Right
        _planes[2] = new Vector4(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42); /// Bottom
        _planes[3] = new Vector4(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42); /// Top
    }

    public bool Intersects (in AABB box) {
        for (int i = 0; i < _planes.Length; i++) {
            Vector4 p = _planes[i];
            float x = 0f <= p.X ? box.Max.X : box.Min.X;
            float y = 0f <= p.Y ? box.Max.Y : box.Min.Y;
            float z = 0f <= p.Z ? box.Max.Z : box.Min.Z;
            if (p.X*x + p.Y*y + p.Z*z + p.W < 0f) return false;
        }
        return true;
    }
}
