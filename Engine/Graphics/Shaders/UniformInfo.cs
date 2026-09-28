using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


//public enum UniformType {
//    Float,
//    Vector2,
//    Vector3,
//    Vector4,
//    Matrix3,
//    Matrix4,
//}

public struct UniformInfo {
    public string Name;
    public Silk.NET.OpenGL.UniformType Type;
    public int Size;
}
