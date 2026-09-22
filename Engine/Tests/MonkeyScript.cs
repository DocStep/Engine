using Newtonsoft.Json;

namespace Engine;


public class MonkeyScript : Script, IAwake, IUpdate {

    [ChangeStep(1f)] public Vector3 dir = new Vector3(0, 90, 0);
    [DrawColor] public Vector3 startColor = Vector3.One;
    [ChangeStep(1f)] public float colorChangeSpeed = 1f;

    public Transform tr1 = null!;
    public Transform tr2 = null!;

    Graphics.Material? mat;
    public Vector3 color;


    public void Awake () {
        Log.log("MonkeyScript.Awake");
        mat = gameObject.GetComponent<Graphics.MeshComponent>()?.Material;
        mat.SetVector3(Graphics.Shader.Color, startColor);
    }
    public void Update () {
        gameObject.Transform.RotateLocalEuler((float)Time.deltaTime*dir);
        return;
        color = new Vector3()!;
        float time = colorChangeSpeed*(float)Time.time;
        color.X = Mathf.Remap01(MathF.Sin(3f*time), -1, 1);
        color.Y = Mathf.Remap01(MathF.Cos(5f*time), -1, 1);
        color.Z = Mathf.Remap01(MathF.Sin(7f*time), -1, 1);
        mat.SetVector3(Graphics.Shader.Color, color);
    }


}
