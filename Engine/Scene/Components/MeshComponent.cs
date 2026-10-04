using Newtonsoft.Json;

namespace Engine.Graphics;


public class MeshComponent : Component, IUpdate, IUpdateAtFreeze {

    [JsonIgnore] public override string Name => nameof(MeshComponent);

    public Mesh? Mesh = null;
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

        /// One RenderInfo per usemtl group — MaterialOverrides maps a submesh's material name
        /// to the Material to draw it with; falls back to Material when a name has no override.
        for (int i = 0; i < Mesh.SubMeshes.Length; i++) {
            Mesh.SubMesh sub = Mesh.SubMeshes[i];
            Material mat = Materials is not null && i < Materials.Length && Materials[i] is not null
                ? Materials[i]
                : Mesh.DefaultMaterials is not null && i < Mesh.DefaultMaterials.Length
                    ? Mesh.DefaultMaterials[i]
                    : Material;

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
