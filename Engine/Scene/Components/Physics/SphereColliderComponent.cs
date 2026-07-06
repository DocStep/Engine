using Newtonsoft.Json;

namespace Engine;


public class SphereColliderComponent : ColliderComponent {

    [JsonIgnore] public readonly static string typeName = typeof(SphereColliderComponent).Name;

    public Vector3 Position = Vector3.Zero;
    public float Radius = 0.5f;


    public override void Update () {
        if (drawGizmos) {
            Graphics.RenderInfo renderInfo = new Graphics.RenderInfo() {
                pos = Position + owner.Transform.Position,
                rot = Vector3.Zero,
                scale = 2f*Radius*owner.Transform.Scale,

                mesh = Graphics.Renderer.Instance._mesh_SphereWireframe,
                shader = Graphics.Renderer.Instance._sh_Unlit,
                material = Graphics.Renderer.Instance._mat_GizmosG,
                primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
            };
            Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
        }
    }

}
