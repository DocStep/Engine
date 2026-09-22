namespace Engine.Graphics;

public enum RenderPass {
    Opaque,
    Transparent,
    Gizmo,
    UI,
}
public enum RenderFace {
    Front,
    Back,
    Both,
}



public class Material : IAsset<Material> {
    [Newtonsoft.Json.JsonConstructor]
    public Material () { }
    public Material (Shader shader) {
        this.shader = shader;
    }
    public Material (Material material) {
        shader = material.shader;
        floats = new Dictionary<string, float>(material.floats);
        vectors2 = new Dictionary<string, Vector2>(material.vectors2);
        vectors3 = new Dictionary<string, Vector3>(material.vectors3);
        vectors4 = new Dictionary<string, Vector4>(material.vectors4);
        ///textures = (Silk.NET.OpenGL.Texture?[])material.textures.Clone();
    }

    public string Name { get; set; } = nameof (Material);
    public long Id { get; set; }
    public string? Path { get; set; }

    [Hide] public Shader shader = null!;

    [Raw] public readonly Dictionary<string, int> ints = new();
    [Raw] public readonly Dictionary<string, float> floats = new(); 
    [Raw] public readonly Dictionary<string, Vector2> vectors2 = new();
    [Raw] public readonly Dictionary<string, Vector3> vectors3 = new();
    [Raw] public readonly Dictionary<string, Vector4> vectors4 = new();
    ///private readonly Silk.NET.OpenGL.Texture?[] textures = new Silk.NET.OpenGL.Texture?[4];

    /// Render State
    public RenderFace face = RenderFace.Front;
    public bool opaque = true;
    public bool depthTest = true;
    public bool depthWrite = true;
    public RenderPass pass = RenderPass.Opaque;


    public void Apply () {
        foreach (var kv in ints) shader.SetInt(kv.Key, kv.Value);
        foreach (var kv in floats) shader.SetFloat(kv.Key, kv.Value);
        foreach (var kv in vectors2) shader.SetVector2(kv.Key, kv.Value);
        foreach (var kv in vectors3) shader.SetVector3(kv.Key, kv.Value);
        foreach (var kv in vectors4) shader.SetVector4(kv.Key, kv.Value);
        /// texture binding here later

        ApplyCustom();
    }
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
    public Material SetVector4 (string name, Vector4 value) {
        vectors4[name] = value;
        return this;
    }



    public void Save (string path) {
        Path = path;
        Json.Write(path, this);
    }
    public static Material? Load (string path) {
        return Json.Read<Material>(path);
    }


    public void Dispose () {

    }

}
