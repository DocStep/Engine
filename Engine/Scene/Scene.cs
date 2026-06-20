using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Engine.Graphics;

namespace Engine;


internal static class Scene {
    internal record SceneObject (
        Vector3D<float>[] Vertices,
        uint[] Indices,
        Matrix4X4<float> ModelMatrix,
        BVHNode BVH
    );

    internal static readonly List<SceneObject> Objects = new List<SceneObject>();

    internal static void Register (Vector3D<float>[] vertices, uint[] indices, Matrix4X4<float> modelMatrix) {
        var bvh = BVHNode.Build(vertices, indices);
        Objects.Add(new SceneObject(vertices, indices, modelMatrix, bvh));
    }

    internal static void Unregister (Vector3D<float>[] vertices) {
        Objects.RemoveAll(o => o.Vertices == vertices);
    }

    internal static void Clear () => Objects.Clear();
}
