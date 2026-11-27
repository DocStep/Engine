using System.Collections.Generic;
using Assimp;
using Silk.NET.OpenGL;
using AssimpScene = Assimp.Scene;

namespace Engine.Graphics;


public static class FbxLoader {

    private const float FbxCentimetersToMeters = 0.01f;

    public static MeshData Load (string path) {
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        bool correctPlaneOrientation = name.Equals("Plane", StringComparison.OrdinalIgnoreCase)
            || name.Equals("PlaneMaterials", StringComparison.OrdinalIgnoreCase);

        using AssimpContext importer = new AssimpContext();
        AssimpScene scene = importer.ImportFile(path,
            PostProcessSteps.Triangulate
            | PostProcessSteps.GenerateSmoothNormals
            | PostProcessSteps.MakeLeftHanded
            | PostProcessSteps.FlipUVs
            | PostProcessSteps.FlipWindingOrder
            | PostProcessSteps.PreTransformVertices);

        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();
        List<SubMeshRange> subMeshes = new List<SubMeshRange>();

        foreach (Assimp.Mesh source in scene.Meshes) {
            uint vertexOffset = (uint)vertices.Count;
            int indexStart = indices.Count;
            bool hasUVs = 0 < source.TextureCoordinateChannelCount
                && source.TextureCoordinateChannels[0] is not null;

            for (int i = 0; i < source.VertexCount; i++) {
                Vector3D position = source.Vertices[i];
                Vector3D normal = source.HasNormals ? source.Normals[i] : new Vector3D(0f, 1f, 0f);
                Vector3D uv = hasUVs ? source.TextureCoordinateChannels[0][i] : default;
                System.Numerics.Vector3 enginePosition = new System.Numerics.Vector3(position.X, position.Y, position.Z);
                System.Numerics.Vector3 engineNormal = new System.Numerics.Vector3(normal.X, normal.Y, normal.Z);
                if (correctPlaneOrientation) {
                    enginePosition = new System.Numerics.Vector3(-enginePosition.X, enginePosition.Y, -enginePosition.Z);
                    engineNormal = new System.Numerics.Vector3(-engineNormal.X, engineNormal.Y, -engineNormal.Z);
                }
                vertices.Add(new Vertex {
                    Position = enginePosition*FbxCentimetersToMeters,
                    Normal = engineNormal,
                    UV = new System.Numerics.Vector2(uv.X, uv.Y),
                });
            }

            foreach (Face face in source.Faces) {
                if (face.IndexCount != 3) continue;
                foreach (int index in face.Indices)
                    indices.Add(vertexOffset + (uint)index);
            }

            int indexCount = indices.Count - indexStart;
            if (indexCount == 0) continue;

            string materialName = 0 <= source.MaterialIndex && source.MaterialIndex < scene.MaterialCount
                ? scene.Materials[source.MaterialIndex].Name
                : source.Name;
            subMeshes.Add(new SubMeshRange {
                MaterialName = string.IsNullOrWhiteSpace(materialName) ? "default" : materialName,
                IndexStart = indexStart,
                IndexCount = indexCount,
            });
        }

        if (vertices.Count == 0 || indices.Count == 0)
            throw new InvalidDataException($"FBX file contains no triangle mesh data: {path}");

        return new MeshData(vertices.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Triangles) {
            SubMeshes = subMeshes.ToArray(),
        };
    }
}
