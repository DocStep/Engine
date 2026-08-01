using Newtonsoft.Json;

namespace Engine;


public class CapsuleColliderComponent : ColliderComponent {

    [JsonIgnore] public override string Name => nameof(CapsuleColliderComponent);

    public Vector3 Position = Vector3.Zero;
    public float Height = 1f;
    public float Radius = 0.5f;


    public override void Update () { }


    /*public override void DrawInspector () {
        ImGuiNET.ImGui.DragFloat3("Position", ref Position, 0.01f);
        ImGuiNET.ImGui.DragFloat("Height", ref Height, 0.01f);
        ImGuiNET.ImGui.DragFloat("Radius", ref Radius, 0.01f);
    }*/

}
