namespace Engine;


internal class MonkeyScript : Script, IComponentUpdate {

    public void Update () {
        owner.Transform.Rotation += (float)Engine.deltaTime*new Vector3(0, 90, 0);
    }

}
