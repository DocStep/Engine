using Newtonsoft.Json;

namespace Engine;


public class CapsuleColliderComponent : ColliderComponent {

    [JsonIgnore] public override string Name => nameof(CapsuleColliderComponent);

    public Vector3 Position = Vector3.Zero;
    public float Height = 1f;
    public float Radius = 0.5f;


    public override void Update () {
        if (drawGizmos) {
            Graphics.RenderInfo renderInfo = new Graphics.RenderInfo() {
                pos = Position + owner.Transform.Position,
                rot = owner.Transform.Rotation,
                scale = owner.Transform.Scale,

                mesh = Gizmos._mesh_CapsuleWireframe,
                material = Gizmos._mat_GizmosG,
                primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
            };
            Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
        }
    }


    /*public override void DrawInspector () {
        ImGuiNET.ImGui.DragFloat3("Position", ref Position, 0.01f);
        ImGuiNET.ImGui.DragFloat("Height", ref Height, 0.01f);
        ImGuiNET.ImGui.DragFloat("Radius", ref Radius, 0.01f);
    }*/

}
