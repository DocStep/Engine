using System.Linq;
using Newtonsoft.Json;

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
    [JsonConstructor]
    public Material () { }
    public void OnLoaded () {
        CoerceLoaded();
        FillDefaults();
    }
    public Material (Shader shader) {
        this.shader = shader;
        Name = shader.Name;
        FillDefaults();
    }
    public Material (Material material) {
        Name = material.Name;
        shader = material.shader;
        Pass = material.Pass;
        Face = material.Face;
        DepthTest = material.DepthTest;
        DepthWrite = material.DepthWrite;
        uniforms = new Dictionary<string, object>(material.uniforms); /// values are immutable (int/float/vectors)
        textures = new Dictionary<string, Texture>(material.textures);
    }
    public Material Clone () => new Material(this);

    public string Name { get; set; } = nameof(Material);
    [Readonly] public long Id { get; set; }
    public string? Path { get; set; }

    [Hide] public Shader shader = null!;

    [JsonIgnore, Hide] private static int _nextId = 0;
    [JsonIgnore, Readonly] public readonly int Id_Renderer = System.Threading.Interlocked.Increment(ref _nextId);

    [JsonIgnore, Hide] public bool Dirty = true;
    [JsonIgnore, Hide] private int _appliedGeneration = -1;
    [JsonIgnore, Hide] private static Material? _lastApplied;
    [JsonIgnore, Hide] private bool IsUpToDate => _lastApplied == this && !Dirty && _appliedGeneration == shader.Generation;
    [JsonIgnore, Hide] private int _syncedGeneration = -1;

    /// Render State
    public RenderPass Pass = RenderPass.Opaque;
    public RenderFace Face = RenderFace.Front;
    //public bool Opaque = true;
    public bool DepthTest = true;
    public bool DepthWrite = true;

    /// Clone() needs to reassign new instances
    [Raw] public Dictionary<string, object> uniforms = new();
    [Raw] public Dictionary<string, Texture> textures = new();


    public void Apply () {
        if (_syncedGeneration != shader.Generation) {
            FillDefaults();
            _syncedGeneration = shader.Generation;
            Dirty = true;
        }
        if (!IsUpToDate) {
            ApplyUniforms();
            Dirty = false;
            _appliedGeneration = shader.Generation;
            _lastApplied = this;
        }
        ApplyTextures();
        ApplyCustom();
    }

    /// Scalar and vector uniforms. Skipped when nothing changed.
    protected virtual void ApplyUniforms () {
        foreach (var kv in uniforms) {
            switch (kv.Value) {
                case int i: shader.SetInt(kv.Key, i); break;
                case float f: shader.SetFloat(kv.Key, f); break;
                case Vector2 v2: shader.SetVector2(kv.Key, v2); break;
                case Vector3 v3: shader.SetVector3(kv.Key, v3); break;
                case Vector4 v4: shader.SetVector4(kv.Key, v4); break;
            }
        }
    }

    /// Always runs: texture units are global GL state and other passes overwrite them.
    protected virtual void ApplyTextures () {
        foreach (var kv in textures) shader.SetTexture(kv.Key, kv.Value);
    }

    /// Per-draw hook for subclasses. Always runs, even when uniforms were skipped.
    public virtual void ApplyCustom () { }


    public void FillDefaults () {
        foreach (var kv in shader.Defaults) uniforms.TryAdd(kv.Key, kv.Value);
        //foreach (string key in Shader.ReservedGlobalUniforms) uniforms.Remove(key);
    }


    public Material Set (string name, object value) {
        if (!CheckUniform(name)) return this;
        uniforms[name] = value;
        Dirty = true;
        return this;
    }
    public Material SetInt (string name, int value) => Set(name, value);
    public Material SetFloat (string name, float value) => Set(name, value);
    public Material SetVector2 (string name, Vector2 value) => Set(name, value);
    public Material SetVector3 (string name, Vector3 value) => Set(name, value);
    public Material SetVector4 (string name, Vector4 value) => Set(name, value);
    public Material SetTexture (string name, Texture value) {
        textures[name] = value;
        if (name == Shader.uTexture) uniforms[Shader.uHasTexture] = 1;
        Dirty = true;
        return this;
    }


    public T Get<T> (string name, T fallback = default!) =>
        uniforms.TryGetValue(name, out object? v) && v is T t ? t : fallback;

    /// Newtonsoft reads object values back as long/double/JObject.
    /// Convert each one to the type the shader declares.
    void CoerceLoaded () {
        foreach (string key in uniforms.Keys.ToList()) {
            if (!shader.ActiveUniforms.TryGetValue(key, out UniformInfo info)) continue;
            object value = uniforms[key];
            uniforms[key] = info.Type switch {
                Silk.NET.OpenGL.UniformType.Int or Silk.NET.OpenGL.UniformType.Bool => Convert.ToInt32(value),
                Silk.NET.OpenGL.UniformType.Float => Convert.ToSingle(value),
                Silk.NET.OpenGL.UniformType.FloatVec2 => ((Newtonsoft.Json.Linq.JObject)value).ToObject<Vector2>(),
                Silk.NET.OpenGL.UniformType.FloatVec3 => ((Newtonsoft.Json.Linq.JObject)value).ToObject<Vector3>(),
                Silk.NET.OpenGL.UniformType.FloatVec4 => ((Newtonsoft.Json.Linq.JObject)value).ToObject<Vector4>(),
                _ => value,
            };
        }
    }

    private bool CheckUniform (string name) {
        if (shader.ActiveUniforms.ContainsKey(name)) return true;
        Log.log($"Material '{Name}': shader has no uniform '{name}'", LogType.warning);
        return false;
    }

    public void Save (string path) {
        Prune(uniforms);
        Prune(textures);

        Path = path;
        Json.Write(path, this);
    }
    public void Save () => Save(Path ?? $"Assets/Materials/{Name}.json");
    private void Prune<T> (Dictionary<string, T> dict) {
        foreach (string key in dict.Keys.ToList())
            if (!shader.ActiveUniforms.ContainsKey(key)) dict.Remove(key);
    }

    public static Material? Load (string path, int part = 100) {
        return Json.Read<Material>(path);
    }


    public void Dispose () { }

}
