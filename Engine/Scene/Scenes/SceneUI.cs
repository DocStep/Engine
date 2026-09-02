using Engine.Graphics;
using Engine.Graphics.UI;

namespace Engine;


public class SceneUI : Scene {

    public override void Load () {
        GameObject go_camera = new GameObject() { Name = "Camera", };
        go_camera.Transform.Position = new Vector3(-2, 3, -10);
        Camera camera = go_camera.AddComponent<Camera>();

        GameObject go_canvas = new GameObject() { Name = "Canvas", };
        go_canvas.AddComponent<Canvas>();
        Image image;

        Texture tex_imageTest = Texture.Load("src/Images/RGBA_Test.png");
        Texture tex_imageWhite = Texture.Load("src/Images/white.png");

        GameObject go_bg = new GameObject() { Name = "Background" };
        go_bg.Transform.Parent = go_canvas.Transform;
        image = go_bg.AddComponent<Image>();
        image.Texture = tex_imageWhite;
        image.Rect.Pivot = new Vector2(0.5f, 0.5f);
        image.Rect.SetAnchor(AnchorPreset.StretchAll);
        image.Alpha = 0.2f;

        GameObject go_image = new GameObject() { Name = "Image", };
        go_image.Transform.Parent = go_canvas.Transform;
        image = go_image.AddComponent<Image>();
        image.Rect.Pivot = new Vector2(0, 0);
        image.Rect.SetAnchor(AnchorPreset.TopLeft);
        image.Rect.Size = new Vector2(100, 100);
        image.Texture = tex_imageTest;

        GameObject go_button = new GameObject() { Name = "Button" };
        go_button.Transform.Parent = go_canvas.Transform;
        image = go_button.AddComponent<Image>();
        image.Rect.Pivot = new Vector2(0, 0);
        image.Rect.SetAnchor(AnchorPreset.TopLeft);
        image.Rect.Size = new Vector2(100, 100);
        image.Texture = tex_imageTest;
        image.Rect.AnchoredPosition = new Vector2(100, 0);
        Button button = go_button.AddComponent<Button>();
        button.de_Clicked += () => Log.log("clicked");

        GameObject go_text = new GameObject() { Name = "Text" };
        go_text.Transform.Parent = go_canvas.Transform;
        TextComponent text = go_text.AddComponent<TextComponent>();
        text.Rect.Size = new Vector2(200, 50);
        text.AlignV = TextAlignV.Center;
        text.AlignH = TextAlignH.Center;
        text.Text = "Text Component";
    }
    
}
