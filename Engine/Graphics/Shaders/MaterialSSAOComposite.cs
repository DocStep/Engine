using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAOComposite : Material {
    public MaterialSSAOComposite (Shader shader) : base(shader) { }

    public const string uAO = "uAO";


    public override void ApplyCustom () {
        shader.SetInt(uAO, 0);
    }

}
