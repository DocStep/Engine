using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Material {

    public Vector3D<float> Color = Constants.lightGray;
    public Texture? AlbedoMap = null;
    public float Roughness = 0.5f;
    public float Metallic = 0f;
    public float Ambient = 0.08f;


    public void Apply (Shader shader) {
        shader.SetVector3("uColor", Color.X, Color.Y, Color.Z);
        shader.SetFloat("uRoughness", Roughness);
        shader.SetFloat("uMetallic", Metallic);
        shader.SetFloat("uAmbient", Ambient);
        // bind texture if present
    }


}
