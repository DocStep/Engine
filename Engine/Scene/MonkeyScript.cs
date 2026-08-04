namespace Engine;


public class MonkeyScript : Script, IComponentUpdate {

    [ChangeStep(1f)] public Vector3 dir = new Vector3(0, 90, 0);


    public void Update () {
        //owner.Transform.Rotation += (float)Time.deltaTime*dir;
    }

}
