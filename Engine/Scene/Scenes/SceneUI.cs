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

        GameObject go_bg = new GameObject() { Name = "Background" };
        go_bg.Transform.Parent = go_canvas.Transform;
        image = go_bg.AddComponent<Image>();
        image.Load("src/Images/white.png");
        image.Rect.Pivot = new Vector2(0.5f, 0.5f);
        image.Rect.SetAnchor(AnchorPreset.StretchAll);
        image.Alpha = 0.2f;

        GameObject go_image = new GameObject() { Name = "Image", };
        go_image.Transform.Parent = go_canvas.Transform;
        image = go_image.AddComponent<Image>();
        image.Load("src/Images/RGBA_Test.png");
        image.Rect.Pivot = new Vector2(0, 0);
        image.Rect.SetAnchor(AnchorPreset.TopLeft);

        GameObject go_button = new GameObject() { Name = "Button" };
        go_button.Transform.Parent = go_canvas.Transform;
        //go_button.Transform.LocalPosition = new Vector3(20, 20, 0);
        image = go_button.AddComponent<Image>();
        image.Load("src/Images/RGBA_Test.png");
        image.Rect.Pivot = new Vector2(0, 0);
        image.Rect.SetAnchor(AnchorPreset.TopLeft);
        //image.Rect.AnchorMin = new Vector2(0f, 0f);
        //image.Rect.AnchorMax = new Vector2(1f, 1f);
        image.Rect.AnchoredPosition = new Vector2(100, 0);
        Button button = go_button.AddComponent<Button>();
        button.de_Clicked += () => Log.log("clicked");
        
        GameObject go_text = new GameObject() { Name = "Text" };
        go_text.Transform.Parent = go_canvas.Transform;
        //go_button.Transform.LocalPosition = new Vector3(20, 20, 0);
        TextComponent text = go_text.AddComponent<TextComponent>();
        text.Text = "Text Component";
        text.Rect.Size = new Vector2(200, 50);
        text.AlignV = TextAlignV.Center;
        text.AlignH = TextAlignH.Center;
    }
    
}
