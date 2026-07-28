namespace Engine;


public class MonkeyScript : Script, IComponentUpdate {

    public string string1 = "kk";
    public int int1 = 1;
    public float float1 = 1.111f;


    public void Update () {
        owner.Transform.Rotation += (float)Engine.deltaTime*new Vector3(0, 90, 0);
    }

}
