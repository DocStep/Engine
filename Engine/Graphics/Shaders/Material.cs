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


public class Material : IAsset<Material>, IOnLoaded {
    [Newtonsoft.Json.JsonConstructor]
    public Material () { }
    public void OnLoaded () {
        FillDefaults();
    }

    /// Deep clone -- MemberwiseClone alone shares dict references with the
    /// original, so SetX on the clone would silently mutate the source material.
    public Material Clone () {
        var copy = (Material)MemberwiseClone();
        copy.ints = new Dictionary<string, int>(ints);
        copy.floats = new Dictionary<string, float>(floats);
        copy.vectors2 = new Dictionary<string, Vector2>(vectors2);
        copy.vectors3 = new Dictionary<string, Vector3>(vectors3);
        copy.vectors4 = new Dictionary<string, Vector4>(vectors4);
        copy.textures = new Dictionary<string, Texture>(textures);
        return copy;
    }

    public Material (Shader shader) {
        this.shader = shader;
        FillDefaults();
    }
    public Material (Material material) {
        shader = material.shader;
        ints = new Dictionary<string, int>(material.ints);
        floats = new Dictionary<string, float>(material.floats);
        vectors2 = new Dictionary<string, Vector2>(material.vectors2);
        vectors3 = new Dictionary<string, Vector3>(material.vectors3);
        vectors4 = new Dictionary<string, Vector4>(material.vectors4);
        textures = new Dictionary<string, Texture>(material.textures);
    }

    public string Name { get; set; } = nameof(Material);
    public long Id { get; set; }
    public string? Path { get; set; }

    [Hide] public Shader shader = null!;

    static readonly Dictionary<Silk.NET.OpenGL.UniformType, object> TypeDefaults = new() {
        [Silk.NET.OpenGL.UniformType.Float] = 0.5f,
        [Silk.NET.OpenGL.UniformType.FloatVec2] = Vector2.Zero,
        [Silk.NET.OpenGL.UniformType.FloatVec3] = Vector3.One,
        [Silk.NET.OpenGL.UniformType.FloatVec4] = Vector4.One,
        [Silk.NET.OpenGL.UniformType.Int] = 0,
    };
    static readonly Dictionary<string, object> NameOverrides = new() {
        ["uMetallic"] = 0f,
        ["uSmoothness"] = 0.5f,
        ["uReflectionIntensity"] = 1f,
    };

    /// Render State
    public RenderFace face = RenderFace.Front;
    public bool opaque = true;
    public bool depthTest = true;
    public bool depthWrite = true;
    public RenderPass pass = RenderPass.Opaque;

    /// Dicts are no longer readonly -- Clone() needs to reassign new instances
    /// rather than mutate the source's dicts in place.
    [Raw] public Dictionary<string, int> ints = new();
    [Raw] public Dictionary<string, float> floats = new();
    [Raw] public Dictionary<string, Vector2> vectors2 = new();
    [Raw] public Dictionary<string, Vector3> vectors3 = new();
    [Raw] public Dictionary<string, Vector4> vectors4 = new();
    [Raw] public Dictionary<string, Texture> textures = new();

    public void Apply () {
        foreach (var kv in ints) shader.SetInt(kv.Key, kv.Value);
        foreach (var kv in floats) shader.SetFloat(kv.Key, kv.Value);
        foreach (var kv in vectors2) shader.SetVector2(kv.Key, kv.Value);
        foreach (var kv in vectors3) shader.SetVector3(kv.Key, kv.Value);
        foreach (var kv in vectors4) shader.SetVector4(kv.Key, kv.Value);
        foreach (var kv in textures) shader.SetTexture(kv.Key, kv.Value);
        ApplyCustom();
    }
    public virtual void ApplyCustom () { }

    public void FillDefaults () {
        foreach (var info in shader.ActiveUniforms.Values) {
            if (Shader.ReservedUniforms.Contains(info.Name)) continue;

            object def = NameOverrides.TryGetValue(info.Name, out var over) ? over
                : TypeDefaults.TryGetValue(info.Type, out var byType) ? byType
                : null!;
            if (def == null) continue;

            switch (def) {
                case float f: floats.TryAdd(info.Name, f); break;
                case Vector2 v2: vectors2.TryAdd(info.Name, v2); break;
                case Vector3 v3: vectors3.TryAdd(info.Name, v3); break;
                case Vector4 v4: vectors4.TryAdd(info.Name, v4); break;
                case int i: ints.TryAdd(info.Name, i); break;
            }
        }
    }

    public Material SetInt (string name, int value) { ints[name] = value; return this; }
    public Material SetFloat (string name, float value) { floats[name] = value; return this; }
    public Material SetVector2 (string name, Vector2 value) { vectors2[name] = value; return this; }
    public Material SetVector3 (string name, Vector3 value) { vectors3[name] = value; return this; }
    public Material SetVector4 (string name, Vector4 value) { vectors4[name] = value; return this; }
    public Material SetTexture (string name, Texture value) { textures[name] = value; return this; }

    public void Save (string path) {
        Path = path;
        Json.Write(path, this);
    }
    public static Material? Load (string path, int part = 100) {
        return Json.Read<Material>(path);
    }

    public void Dispose () { }
}
