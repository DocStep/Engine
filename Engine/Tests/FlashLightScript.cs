using Newtonsoft.Json;

namespace Engine;


public class FlashLightScript : Script, IAwake, IUpdate {

    [DrawColor] public Vector3 startColor = Vector3.One;
    [ChangeStep(1f)] public float colorChangeSpeed = 100f;

    public Transform tr1 = null!;
    public Transform tr2 = null!;

    Graphics.PointLight? light;
    public Vector3 color;


    public void Awake () {
        light ??= gameObject.GetComponent<Graphics.PointLight>();
    }
    public void Update () {
        //color = new Vector3()!;
        float time = colorChangeSpeed*(float)Time.time;
        color.X = Mathf.Remap01(MathF.Sin(3f*time), -1, 1);
        color.Y = Mathf.Remap01(MathF.Cos(5f*time), -1, 1);
        color.Z = Mathf.Remap01(MathF.Sin(7f*time), -1, 1);
        //light.Intensity = (Graphics.Shader.Color, color);

        light?.Intensity = 10*color.X*color.Y*color.Z;

        return;
    }


}
