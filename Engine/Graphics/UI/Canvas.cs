using Silk.NET.OpenGL;
using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public class Canvas : Component, IComponentDrawRaw {

    public override string Name => nameof(Canvas);

    [Hide][JsonIgnore] private GL GL => Renderer.GL;
    
    [Hide][JsonIgnore] public Matrix4x4 m4x4_View = Matrix4x4.Identity;
    [Hide][JsonIgnore] public Matrix4x4 m4x4_Projection = Matrix4x4.Identity;


    public void DrawRaw () {
        m4x4_Projection = Matrix4x4.CreateOrthographicOffCenter(0, Renderer.Instance.Width, Renderer.Instance.Height, 0, -1f, 1f);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);

        DrawSelfAndChildren(gameObject.Transform);

        GL.Enable(EnableCap.DepthTest);
    }

    private void DrawSelfAndChildren (Transform tr) {
        if (!tr.Enabled) return;

        Button? button = tr.gameObject.GetComponent<Button>();
        button?.UpdateInput();

        Image? image = tr.gameObject.GetComponent<Image>();
        if (image is not null && image.Enabled)
            image.Draw(m4x4_Projection);

        foreach (Transform child in tr.Children)
            DrawSelfAndChildren(child);
    }


    public GameObject? Pick (Vector2 mousePos) {
        return PickChildren(gameObject.Transform, mousePos);
    }

    private GameObject? PickChildren (Transform t, Vector2 mousePos) {
        /// reverse order: last-drawn (topmost) children get priority
        for (int i = t.Children.Count - 1; i >= 0; i--) {
            Transform child = t.Children[i];
            if (!child.Enabled) continue;

            GameObject? hit = PickChildren(child, mousePos);
            if (hit is not null) return hit;

            Image? image = child.gameObject.GetComponent<Image>();
            if (image is null || !image.Enabled) continue;

            Vector3 pos = child.Position;
            bool inside =
                pos.X <= mousePos.X && mousePos.X <= pos.X + image.Size.X &&
                pos.Y <= mousePos.Y && mousePos.Y <= pos.Y + image.Size.Y;

            if (inside) return child.gameObject;
        }

        return null;
    }

}