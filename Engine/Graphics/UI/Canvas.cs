using Silk.NET.OpenGL;
using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public class Canvas : Component, IComponentDrawRaw {

    public override string Name => nameof(Canvas);

    [Hide][JsonIgnore] private GL GL => Renderer.GL;

    [Hide]
    [JsonIgnore]
    public Matrix4x4 Projection {
        get {
            return Matrix4x4.CreateOrthographicOffCenter(0, Renderer.Instance.Width, Renderer.Instance.Height, 0, -1f, 1f);
        }
    }

    public void DrawRaw () {
        Log.log("Canvas", "Draw");
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);

        DrawSelfAndChildren(gameObject.Transform);

        GL.Enable(EnableCap.DepthTest);
    }

    private void DrawSelfAndChildren (Transform t) {
        if (!t.Enabled) return;

        Image? image = t.gameObject.GetComponent<Image>();
        if (image is not null && image.Enabled)
            image.Draw(Projection);

        foreach (Transform child in t.Children)
            DrawSelfAndChildren(child);
    }

}