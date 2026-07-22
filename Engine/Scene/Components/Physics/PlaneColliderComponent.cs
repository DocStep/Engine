using Newtonsoft.Json;

namespace Engine;


public class PlaneColliderComponent : ColliderComponent {

    [JsonIgnore] public readonly static string typeName = typeof(PlaneColliderComponent).Name;

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;


    public override void Update () {
        if (drawGizmos) {
            Graphics.RenderInfo renderInfo = new Graphics.RenderInfo() {
                pos = Position + owner.Transform.Position,
                rot = Rotation + owner.Transform.Rotation,
                scale = Scale*owner.Transform.Scale,

                mesh = Gizmos._mesh_PlaneWireframe,
                material = Gizmos._mat_GizmosG,
                primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
            };
            Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
        }
    }

}
