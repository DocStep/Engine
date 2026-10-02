using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialReflection : Material {
    public MaterialReflection (Shader shader) : base(shader) { }

    public override void ApplyCustom () {
        if (Renderer.Instance.Camera is null) return;
        if (Renderer.Instance.Skybox.texture is null) return;

        Log.log("MaterialReflection");
        //Renderer.Instance.SetSceneUniformsUnlit(shader, Renderer.Instance.Camera.CameraPos);
        //shader.SetVector3(Shader.uViewPos, Renderer.Instance.Camera.CameraPos);


        Renderer.Instance.Skybox.texture.Bind(TextureUnit.Texture6); /// use whatever unit Lit uses for the skybox
        shader.SetInt(Shader.uSkybox, 6);
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);    /// Texture.Bind leaves the unit active; restore
    }

}
