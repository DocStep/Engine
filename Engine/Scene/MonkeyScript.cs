namespace Engine;


public class MonkeyScript : Script, IComponentAwake, IComponentUpdate {

    [ChangeStep(1f)] public Vector3 dir = new Vector3(0, 90, 0);

    Graphics.Material? mat;
    public Vector3 color;


    public void Awake () {
        Log.log("MonkeyScript.Awake");
        mat = gameObject.GetComponent<Graphics.MeshComponent>()?.material;
    }
    public void Update () {
        gameObject.Transform.LocalRotation += (float)Time.deltaTime*dir;

        color = new Vector3()!;
        color.X = Mathf.Remap01(MathF.Sin(3f*(float)Time.time), -1, 1);
        color.Y = Mathf.Remap01(MathF.Cos(5f*(float)Time.time), -1, 1);
        color.Z = Mathf.Remap01(MathF.Sin(7f*(float)Time.time), -1, 1);
        mat.SetVector3(Graphics.Shader.Color, color);
    }


}
