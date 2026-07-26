namespace Engine;


public class MonkeyScript : Script, IComponentUpdate {

    public string call = "kk";
    public int count = 1;
    public float val = 1.111f;


    public void Update () {
        owner.Transform.Rotation += (float)Engine.deltaTime*new Vector3(0, 90, 0);
    }

}
