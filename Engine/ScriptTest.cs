namespace Engine;


public class ScriptTest : IActionScript_Update {

    public void OnScriptAction_Update () {
        if (SceneManager.ActiveScene.Objects.Count == 0) return;
        SceneManager.ActiveScene.Objects[1].Transform.Rotation += (float)Time.deltaTime*new Vector3(90, 0, 0);
    }

}
