using System.Runtime.InteropServices;

namespace Engine.Graphics;


[StructLayout(LayoutKind.Sequential)]
public struct ParticleInstance {
    public Vector3 Position;
    public float Scale;
    public Quaternion Rotation; /// or just a single float angle for billboards
    public Vector4 Color;
}
