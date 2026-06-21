using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine.Graphics;


internal static class Constants {

    public readonly static Vector3D<float> white = new Vector3D<float>(1, 1, 1);
    public readonly static Vector3D<float> black = new Vector3D<float>(0, 0, 0);

    public readonly static Vector3D<float> lightGray = new Vector3D<float>(0.8f, 0.8f, 0.8f);
    public readonly static Vector3D<float> gray = new Vector3D<float>(0.5f, 0.5f, 0.5f);
    public readonly static Vector3D<float> darkGray = new Vector3D<float>(0.25f, 0.25f, 0.25f);

    public readonly static Vector3D<float> red = new Vector3D<float>(1f, 0f, 0f);
    public readonly static Vector3D<float> green = new Vector3D<float>(0f, 1f, 0f);
    public readonly static Vector3D<float> blue = new Vector3D<float>(0f, 0f, 1f);

    public readonly static Vector3D<float> magenta = new Vector3D<float>(1f, 0f, 1f);
    public readonly static Vector3D<float> yellow = new Vector3D<float>(1f, 1f, 0f);
    public readonly static Vector3D<float> cyan = new Vector3D<float>(0f, 1f, 1f);


}
