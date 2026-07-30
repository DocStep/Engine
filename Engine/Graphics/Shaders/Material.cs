namespace Engine.Graphics;

public enum RenderPass {
    Opaque,
    Transparent,
    UI,
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
        vectors2 = new Dictionary<string, Vector2>(material.vectors2);
        vectors3 = new Dictionary<string, Vector3>(material.vectors3);
        ///textures = (Silk.NET.OpenGL.Texture?[])material.textures.Clone();
    }
    public Shader shader = default!;
    private readonly Dictionary<string, int> ints = new();
    private readonly Dictionary<string, float> floats = new();
    private readonly Dictionary<string, Vector2> vectors2 = new();
    private readonly Dictionary<string, Vector3> vectors3 = new();
    ///private readonly Silk.NET.OpenGL.Texture?[] textures = new Silk.NET.OpenGL.Texture?[4];

    /// Render State
    public RenderPass pass = RenderPass.Opaque;
    public RenderFace face = RenderFace.Front;
    public bool opaque = true;
    public bool depthTest = true;
    public bool depthWrite = true;


    public virtual void ApplyCustom () { }


    public Material SetInt (string name, int value) {
        ints[name] = value;
        return this;
    }
    public Material SetFloat (string name, float value) {
        floats[name] = value;
        return this;
    }
    public Material SetVector2 (string name, Vector2 value) {
        vectors2[name] = value;
        return this;
    }
    public Material SetVector3 (string name, Vector3 value) {
        vectors3[name] = value;
        return this;
    }

    public void Apply () {
        foreach (var kv in ints) shader.SetFloat(kv.Key, kv.Value);
        foreach (var kv in floats) shader.SetFloat(kv.Key, kv.Value);
        foreach (var kv in vectors2) shader.SetVector2(kv.Key, kv.Value);
        foreach (var kv in vectors3) shader.SetVector3(kv.Key, kv.Value);
        /// texture binding here later
        
        ApplyCustom();
    }


    /*public void Dispose () {
        
    }*/

}