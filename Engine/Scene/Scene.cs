using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Engine.Graphics;

namespace Engine;


internal static class Scene {
    internal record SceneObject (
        Vector3[] Vertices,
        uint[] Indices,
        Matrix4x4 ModelMatrix,
        BVHNode BVH
    );

    internal static readonly List<SceneObject> Objects = new List<SceneObject>();

    internal static void Register (Vector3[] vertices, uint[] indices, Matrix4x4 modelMatrix) {
        var bvh = BVHNode.Build(vertices, indices);
        Objects.Add(new SceneObject(vertices, indices, modelMatrix, bvh));
    }

    internal static void Unregister (Vector3[] vertices) {
        Objects.RemoveAll(o => o.Vertices == vertices);
    }

    internal static void Clear () => Objects.Clear();
}
