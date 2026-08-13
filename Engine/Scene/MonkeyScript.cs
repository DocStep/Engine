namespace Engine;


public class MonkeyScript : Script, IComponentAwake, IComponentUpdate {

    [ChangeStep(1f)] public Vector3 dir = new Vector3(0, 90, 0);

    Graphics.Material? mat;
    public Vector3 color;


    public void Awake () {
        Log.log("MonkeyScript.Awake");
        mat = owner.GetComponent<Graphics.MeshComponent>()?.material;
    }
    public void Update () {
        owner.Transform.RotationEuler += (float)Time.deltaTime*dir;

        //mat = owner.GetComponent<Graphics.MeshComponent>()?.material;
        color = new Vector3()!;
        color.X = Mathf.Remap01(MathF.Sin(3f*(float)Time.time), -1, 1);
        color.Y = Mathf.Remap01(MathF.Cos(5f*(float)Time.time), -1, 1);
        color.Z = Mathf.Remap01(MathF.Sin(7f*(float)Time.time), -1, 1);
        mat.SetVector3(Graphics.Shader.Color, color);
    }


}
