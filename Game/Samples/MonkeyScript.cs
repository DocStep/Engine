using Engine;
using Engine.Graphics;

namespace Game.Samples;


public class MonkeyScript : Script, IAwake, IUpdate {

    [ChangeStep(1f)] public Vector3 dir = new Vector3(0, 90, 0);
    [DrawColor] public Vector3 startColor = Vector3.One;
    [ChangeStep(1f)] public float colorChangeSpeed = 1f;

    public Transform tr1 = null!;
    public Transform tr2 = null!;

    Material? mat;
    public Vector3 color;


    public void Awake () {
        Log.log("MonkeyScript.Awake");
        mat = gameObject.GetComponent<MeshComponent>()?.Material;
        if (mat is null) return;
        if (mat.Has(Shader.uColor))
            mat?.SetVector3(Shader.uColor, startColor);
    }
    public void Update () {
        gameObject.Transform.RotateLocalEuler((float)Time.deltaTime*dir);

        //color = new Vector3()!;
        //float time = colorChangeSpeed*(float)Time.time;
        //color.X = Mathf.Remap01(MathF.Sin(3f*time), -1, 1);
        //color.Y = Mathf.Remap01(MathF.Cos(5f*time), -1, 1);
        //color.Z = Mathf.Remap01(MathF.Sin(7f*time), -1, 1);
        //mat.SetVector3(Shader.Color, color);

        return;
    }


}
