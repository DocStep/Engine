namespace Engine;


public class ScriptTest : IActionScript_Update {

    public void Update_AS () {
        if (SceneManager.ActiveScene.Objects.Count < 2) return;
        GameObject? Suzanne = SceneManager.ActiveScene.Find("Reflection Suzanne");
        if (Suzanne is null) return;

        Suzanne.Transform.RotationEuler += (float)Time.deltaTime*new Vector3(90, 0, 0);
    }

}
