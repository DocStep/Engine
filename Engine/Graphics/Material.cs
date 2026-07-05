namespace Engine.Graphics;


public class Material {

    public Shader? shader = null;

    public Vector3 Color = Constants.lightGray;
    public Silk.NET.OpenGL.Texture? AlbedoMap = null;
    public float Roughness = 0.5f;
    public float Metallic = 0f;
    public float Ambient = 0.08f;
    public float Alpha = 1f;


    public void Apply (Shader shader) {
        shader.SetVector3("uColor", Color.X, Color.Y, Color.Z);
        shader.SetFloat("uRoughness", Roughness);
        shader.SetFloat("uMetallic", Metallic);
        shader.SetFloat("uAmbient", Ambient);
        shader.SetFloat("uAlpha", Alpha);

        /// Bind texture if present
    }


}
