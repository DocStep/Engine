using Engine.Graphics;
using Engine.Graphics.UI;

namespace Engine;


public class SceneUI : Scene {

    public override void Load () {
        GameObject go_camera = new GameObject() { Name = "Camera", };
        go_camera.Transform.Position = new Vector3(-2, 3, -10);
        Camera camera = go_camera.AddComponent<Camera>();

        GameObject go_ui = new GameObject() { Name = "UI", };
        go_ui.AddComponent<Canvas>();
        Image image = go_ui.AddComponent<Image>();
        image.Load("src/Images/RGBA_Test.png");
        image.Size = new Vector2(64, 64);

        GameObject go_button = new GameObject() { Name = "PlayButton" };
        go_button.Transform.LocalPosition = new Vector3(20, 20, 0);
        image = go_button.AddComponent<Image>();
        image.Load("src/Images/RGBA_Test.png");
        image.Size = new Vector2(120, 40);
        Button button = go_button.AddComponent<Button>();
        button.de_Clicked += () => Log.log("clicked");
        go_ui.Transform.Children.Add(go_button.Transform);

    }
    
}
