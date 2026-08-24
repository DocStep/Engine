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

        GameObject go_image = new GameObject() { Name = "UI", };
        go_image.Transform.Parent = go_canvas.Transform;
        image = go_image.AddComponent<Image>();
        image.Load("src/Images/RGBA_Test.png");
        image.Rect.Pivot = new Vector2(0, 0);
        image.Rect.Anchor = new Vector2(0, 0);

        GameObject go_button = new GameObject() { Name = "PlayButton" };
        go_button.Transform.Parent = go_canvas.Transform;
        //go_button.Transform.LocalPosition = new Vector3(20, 20, 0);
        image = go_button.AddComponent<Image>();
        image.Load("src/Images/RGBA_Test.png");
        image.Rect.Pivot = new Vector2(0, 0);
        image.Rect.Anchor = new Vector2(0, 0);
        image.Rect.AnchoredPosition = new Vector2(100, 0);
        Button button = go_button.AddComponent<Button>();
        button.de_Clicked += () => Log.log("clicked");
    }
    
}
