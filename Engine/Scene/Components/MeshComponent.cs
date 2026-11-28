using Newtonsoft.Json;

namespace Engine.Graphics;


public class MeshComponent : Component, IUpdateAtFreeze {

    [Hide, JsonIgnore] private Mesh? mesh;
    [JsonProperty("Mesh")]
    public Mesh? Mesh {
        get => mesh;
        set {
            if (ReferenceEquals(mesh, value)) return;
            mesh = value;
            if (_gameObject is not null)
                _gameObject.GetComponent<global::Engine.MeshColliderComponent>()?.SetMesh(value);
        }
    }
    public Material? Material = AssetsEngine._mat_Lit;
    public Material[]? Materials;


    public void Update () {
        if (Renderer.Instance.Camera is null || Mesh is null || Material is null) return;

        Matrix4x4 model = gameObject.Transform.GetWorldMatrix();
        Silk.NET.OpenGL.PrimitiveType primitiveType = Mesh.Data is not null ? Mesh.Data.PrimitiveType : default;

        if (Mesh.SubMeshes.Length <= 1) {
            RenderData renderData = new RenderData() {
                model = model,
                mesh = LOD.GetLOD(Mesh, Vector3.DistanceSquared(gameObject.Transform.Position, Renderer.Instance.Camera.CameraPos)),
                material = Material,
                primitiveType = primitiveType,
            };
            Renderer.Instance.AddRenderData(renderData);
            return;
        }

        /// Match scene overrides by material name because FBX submesh ordering can differ from OBJ.
        for (int i = 0; i < Mesh.SubMeshes.Length; i++) {
            Mesh.SubMesh sub = Mesh.SubMeshes[i];
            Material? overrideMaterial = null;
            if (Materials is not null) {
                foreach (Material? candidate in Materials) {
                    if (candidate?.Name == sub.MaterialName) {
                        overrideMaterial = candidate;
                        break;
                    }
                }
            }

            Material mat = overrideMaterial
                ?? (Mesh.DefaultMaterials is not null && i < Mesh.DefaultMaterials.Length
                    ? Mesh.DefaultMaterials[i]
                    : Material);

            RenderData renderData = new RenderData() {
                model = model,
                mesh = LOD.GetLOD(Mesh, Vector3.DistanceSquared(gameObject.Transform.Position, Renderer.Instance.Camera.CameraPos)),
                material = mat,
                primitiveType = primitiveType,
                indexOffset = sub.IndexOffset,
                indexCount = sub.IndexCount,
            };
            Renderer.Instance.AddRenderData(renderData);
        }
    }

}
