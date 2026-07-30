using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAOComposite : Material {
    public MaterialSSAOComposite (Shader shader) : base(shader) { }

    public uint originalSceneTexture; /// set once per frame, before AO ping-pong overwrites currentInput

    public const string Original = "uOriginal";

    public override void ApplyCustom () {
        Renderer.GL.ActiveTexture(TextureUnit.Texture2);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, originalSceneTexture);
        shader.SetInt(Original, 2); /// uAO = unit 2, uScene stays unit 0 (blurred AO from chain)
    }
}
