namespace Engine.Graphics;

public enum RenderPass {
    undefined = -1,
    Opaque,
    Transparent,
    Gizmo,
    UI
}
public enum RenderFace {
    Front,
    Back,
    Both,
}


public class Material /*: IDisposable*/ {
    //public Material () { }
    public Material (Shader shader) {
        this.shader = shader;
    }
    public Material (Material material) {
        shader = material.shader;
        floats = new Dictionary<string, float>(material.floats);
        vectors = new Dictionary<string, Vector3>(material.vectors);
        ///textures = (Silk.NET.OpenGL.Texture?[])material.textures.Clone();
    }
    public Shader shader = default!;

    private readonly Dictionary<string, float> floats = new();
    private readonly Dictionary<string, Vector3> vectors = new();
    ///private readonly Silk.NET.OpenGL.Texture?[] textures = new Silk.NET.OpenGL.Texture?[4];

    /// Render State
    public RenderPass pass = RenderPass.Opaque;
    public RenderFace face = RenderFace.Front;
    public bool opaque = true;
    public bool depthTest = true;
    public bool depthWrite = true;


    public Material SetFloat (string name, float value) {
        floats[name] = value;
        return this;
    }
    public Material SetVector3 (string name, Vector3 value) {
        vectors[name] = value;
        return this;
    }

    public void Apply (Shader shader) {
        foreach (var kv in floats) shader.SetFloat(kv.Key, kv.Value);
        foreach (var kv in vectors) shader.SetVector3(kv.Key, kv.Value);
        /// texture binding here later
    }


    /*public void Dispose () {
        
    }*/

}