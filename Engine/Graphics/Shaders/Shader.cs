using System.Globalization;
using System.Text.RegularExpressions;
using Silk.NET.OpenGL;
using Newtonsoft.Json;

namespace Engine.Graphics;


public class Shader : IAsset<Shader> {
    public Shader (string vertexSourcePath, string fragmentSourcePath, string name = "unnamed", bool isLit = true) {
        GL = Renderer.GL;
        Name = name;
        this.isLit = isLit;

        this.VertexSourcePath = vertexSourcePath;
        this.FragmentSourcePath = fragmentSourcePath;

        Compile();
    }

    public void Compile () {
        string vertexSource = Assets.LoadText(VertexSourcePath);
        string fragmentSource = Assets.LoadText(FragmentSourcePath);

        uint vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        //try {
        //    vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        //} catch (Exception ex) {
        //    throw new Exception($"Failed to compile Shader_Vertex {VertexSourcePath}", ex);
        //}
        //try {
        //    fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        //} catch (Exception ex) {
        //    throw new Exception($"Failed to compile Shader_Fragment {FragmentSourcePath}", ex);
        //}

        uint program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);

        GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        if (status == 0) {
            string log = GL.GetProgramInfoLog(program);
            GL.DeleteProgram(program);
            throw new Exception($"Shader program failed to link: {log}");
        }

        uint oldProgram = _program;
        _program = program;
        _locations.Clear(); /// locations belong to the old program
        Generation++;       /// lets Material know its uniforms must be re-uploaded
        ReflectUniforms();
        BuildDefaults(vertexSource, fragmentSource);
        GL.DeleteProgram(oldProgram);
    }
    uint CompileShader (ShaderType type, string source) {
        uint shaderId = GL.CreateShader(type);
        GL.ShaderSource(shaderId, source);
        GL.CompileShader(shaderId);

        GL.GetShader(shaderId, ShaderParameterName.CompileStatus, out int status);
        if (status == 0) {
            string log = GL.GetShaderInfoLog(shaderId);
            GL.DeleteShader(shaderId);
            throw new Exception($"{type} failed to compile: {log}");
        }

        return shaderId;
    }
    public void Rebind () {

    }

    /*public Shader (Shader shader) : this(shader._vertexSource, shader._fragmentSource, shader.Name + " (copy)") {
        pass = shader.pass;
        depthTest = shader.depthTest;
        depthWrite = shader.depthWrite;
    }*/

    public string Name { get; set; } = "Unnamed";
    public long Id { get; set; }
    public string? Path { get; set; }

    [JsonProperty] private readonly string VertexSourcePath;
    [JsonProperty] private readonly string FragmentSourcePath;
    public bool isLit;

    [JsonIgnore, Hide] private readonly Dictionary<string, int> _locations = new();
    [JsonIgnore, Hide] public int Generation { get; private set; }

    [JsonIgnore, Hide] private readonly GL GL;
    [JsonIgnore, Hide] public uint _program { get; private set; }
    [JsonIgnore, Hide] private readonly Dictionary<string, int> _textureUnits = new();
    [JsonIgnore, Hide] private int nextTextureUnit = 1; /// 0 is permanently reserved for uSkybox

    [JsonIgnore, Hide] private static int _nextId = 0;
    [JsonIgnore, Hide] public readonly int Id_Renderer = System.Threading.Interlocked.Increment(ref _nextId);

    [JsonIgnore, Hide]
    public Dictionary<string, UniformInfo> ActiveUniforms { get; private set; } = new Dictionary<string, UniformInfo>();

    /// Uniform names the renderer sets globally per-frame/per-pass (camera, lights, time...).
    /// FillDefaults must never touch these -- they don't belong to any one material.
    [JsonIgnore, Hide]
    public static readonly HashSet<string> ReservedGlobalUniforms = new HashSet<string>() {
        uView, uProjection, uViewPos, uModel, uNormalMatrix, uCameraPos, uScene, uDepth,
        uSunLightCount, uSunLightDir, uSunLightColor, uSunLightIntensity,
        uPointLightCount, uPointLightColor, uPointLightIntensity, uPointLightPos, uPointLightRange,
        uSkybox, uEnvMap, uRoughness, uMaxReflectionLod,
        uEnvIntensity,
        uAmbientMode, 
        uAmbientColor, uAmbientColorIntensity, 
        uAmbientTop, uAmbientMiddle, uAmbientBottom, uAmbientIntensity, 
        uReflectionIntensity,
        uSHAr, uSHAg, uSHAb, uSHBr, uSHBg, uSHBb, uSHC,

        uSunShadow, uPointShadowSlot, uPointShadowMap,
        uShadowMap, uLightSpace, uShadowTexelWorld, uShadowBias, uShadowNormalOffset,
        uShadowAmbientSpecular, uShadowAmbientDiffuse,

        uExposure,
    };


    [JsonIgnore, Hide] 
    public Dictionary<string, object> Defaults { get; private set; } = new();

    //[JsonIgnore, Hide]
    //public readonly static Dictionary<UniformType, object> UniformTypeDefaults = new Dictionary<UniformType, object>();
    //[JsonIgnore, Hide]
    //public readonly static Dictionary<string, object> UniformDefaults = new Dictionary<string, object>();

    [JsonIgnore] public static RendererGLStats Stats = default;

    [JsonIgnore, Hide] public const int SkyboxUnitIndex = 0;
    [JsonIgnore, Hide] public const int TextureUnitIndex = 10;

    [JsonIgnore, Hide] public const string uView = "uView";
    [JsonIgnore, Hide] public const string uProjection = "uProjection";
    [JsonIgnore, Hide] public const string uInvProjection = "uInvProjection";
    [JsonIgnore, Hide] public const string uViewPos = "uViewPos";
    [JsonIgnore, Hide] public const string uModel = "uModel";
    [JsonIgnore, Hide] public const string uNormalMatrix = "uNormalMatrix";
    [JsonIgnore, Hide] public const string uCameraPos = "uCameraPos";
    [JsonIgnore, Hide] public const string uScene = "uSceneColor";
    [JsonIgnore, Hide] public const string uDepth = "uDepth";

    [JsonIgnore, Hide] public const string uSunLightCount = "uSunLightCount";
    [JsonIgnore, Hide] public const string uSunLightDir = "uSunLightDir";
    [JsonIgnore, Hide] public const string uSunLightColor = "uSunLightColor";
    [JsonIgnore, Hide] public const string uSunLightIntensity = "uSunLightIntensity";

    [JsonIgnore, Hide] public const string uPointLightCount = "uPointLightCount";
    [JsonIgnore, Hide] public const string uPointLightColor = "uPointLightColor";
    [JsonIgnore, Hide] public const string uPointLightIntensity = "uPointLightIntensity";
    [JsonIgnore, Hide] public const string uPointLightPos = "uPointLightPos";
    [JsonIgnore, Hide] public const string uPointLightRange = "uPointLightRange";

    [JsonIgnore, Hide] public const string uSkybox = "uSkybox";
    [JsonIgnore, Hide] public const string uEnvMap = "uEnvMap";
    [JsonIgnore, Hide] public const string uMaxReflectionLod = "uMaxReflectionLod";

    [JsonIgnore, Hide] public const string uExposure = "uExposure";
    [JsonIgnore, Hide] public const string uTonemapMode = "uTonemapMode";

    [JsonIgnore, Hide] public const string uEnvIntensity = "uEnvIntensity";
    [JsonIgnore, Hide] public const string uAmbientMode = "uAmbientMode";
    [JsonIgnore, Hide] public const string uAmbientTop = "uAmbientTop";
    [JsonIgnore, Hide] public const string uAmbientMiddle = "uAmbientMiddle";
    [JsonIgnore, Hide] public const string uAmbientBottom = "uAmbientBottom";
    [JsonIgnore, Hide] public const string uAmbientIntensity = "uAmbientIntensity";
    [JsonIgnore, Hide] public const string uAmbientColor = "uAmbientColor";
    [JsonIgnore, Hide] public const string uAmbientColorIntensity = "uAmbientColorIntensity";
    [JsonIgnore, Hide] public const string uReflectionIntensity = "uReflectionIntensity";

    //[JsonIgnore, Hide] public const string uShadowEnabled = "uShadowEnabled";

    [JsonIgnore, Hide] public const string uSunShadow = "uSunShadow";
    [JsonIgnore, Hide] public const string uPointShadowSlot = "uPointShadowSlot";
    [JsonIgnore, Hide] public const string uPointShadowMap = "uPointShadowMap";
    [JsonIgnore, Hide] public const string uShadowMap = "uShadowMap";
    [JsonIgnore, Hide] public const string uLightSpace = "uLightSpace";
    [JsonIgnore, Hide] public const string uShadowTexelWorld = "uShadowTexelWorld";
    [JsonIgnore, Hide] public const string uShadowSoftness = "uShadowSoftness";
    [JsonIgnore, Hide] public const string uShadowBias = "uShadowBias";
    [JsonIgnore, Hide] public const string uShadowNormalOffset = "uShadowNormalOffset";
    [JsonIgnore, Hide] public const string uShadowAmbientSpecular = "uShadowAmbientSpecular";
    [JsonIgnore, Hide] public const string uShadowAmbientDiffuse = "uShadowAmbientDiffuse";
    [JsonIgnore, Hide] public const string uShadowLightIndex = "uShadowLightIndex";

    [JsonIgnore, Hide] public const string uSHAr = "uSHAr";
    [JsonIgnore, Hide] public const string uSHAg = "uSHAg";
    [JsonIgnore, Hide] public const string uSHAb = "uSHAb";
    [JsonIgnore, Hide] public const string uSHBr = "uSHBr";
    [JsonIgnore, Hide] public const string uSHBg = "uSHBg";
    [JsonIgnore, Hide] public const string uSHBb = "uSHBb";
    [JsonIgnore, Hide] public const string uSHC = "uSHC";

    [JsonIgnore, Hide] public const string uColor = "uColor";
    [JsonIgnore, Hide] public const string uTexture = "uTexture";
    [JsonIgnore, Hide] public const string uHasTexture = "uHasTexture";
    [JsonIgnore, Hide] public const string uSmoothness = "uSmoothness";
    [JsonIgnore, Hide] public const string uRoughness = "uRoughness";
    [JsonIgnore, Hide] public const string uMetallic = "uMetallic";
    [JsonIgnore, Hide] public const string uAlpha = "uAlpha";
    [JsonIgnore, Hide] public const string uRadius = "uRadius";
    [JsonIgnore, Hide] public const string uBias = "uBias";
    [JsonIgnore, Hide] public const string uFade = "uFade";
    [JsonIgnore, Hide] public const string uBlurScale = "uBlurScale";
    [JsonIgnore, Hide] public const string uStrength = "uStrength";
    [JsonIgnore, Hide] public const string uPower = "uPower";
    [JsonIgnore, Hide] public const string uTint = "uTint";
    [JsonIgnore, Hide] public const string uNear = "uNear";
    [JsonIgnore, Hide] public const string uFar = "uFar";
    [JsonIgnore, Hide] public const string uNormal = "uNormal";
    [JsonIgnore, Hide] public const string uSampleCount = "uSampleCount";
    [JsonIgnore, Hide] public const string uFalloffPower = "uFalloffPower";
    [JsonIgnore, Hide] public const string uTexelSize = "uTexelSize";
    [JsonIgnore, Hide] public const string uInvResolution = "uInvResolution";


    static readonly Regex CommentRx = new(@"//.*?$|/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);
    static readonly Regex UniformDeclRx = new(@"\buniform\s+(?:(?:highp|mediump|lowp)\s+)?(\w+)\s+([^;]+);", RegexOptions.Compiled);


    public void Use () {
        GL.UseProgram(_program);
        //_nextTextureUnit = 0;
        //GLEnum err = GL.GetError();
        //if (err != GLEnum.NoError) 
        //    Console.WriteLine($"UseProgram({_program}, {Name}) Error: {err}");
    }



    void BuildDefaults (string vertexSource, string fragmentSource) {
        Defaults.Clear();

        Dictionary<string, object> shaderDefaults = ParseUniformDefaults(vertexSource);
        foreach (KeyValuePair<string, object> pair in ParseUniformDefaults(fragmentSource))
            shaderDefaults[pair.Key] = pair.Value;

        foreach (UniformInfo info in ActiveUniforms.Values) {
            if (ReservedGlobalUniforms.Contains(info.Name)) continue;
            if (info.Size != 1) {
                Log.log($"Shader '{Name}': array uniform '{info.Name}' is not supported", LogType.warning);
                continue;
            }

            if (shaderDefaults.TryGetValue(info.Name, out object? shaderValue))
                Defaults[info.Name] = shaderValue;
        }
    }


    Dictionary<string, object> ParseUniformDefaults (string source) {
        Dictionary<string, object> defaults = new();
        string clean = CommentRx.Replace(source, " ");

        foreach (Match m in UniformDeclRx.Matches(clean)) {
            string type = m.Groups[1].Value;

            /// handles `uniform float a = 1.0, b = 2.0;`
            foreach (string decl in SplitTopLevel(m.Groups[2].Value)) {
                int eq = decl.IndexOf('=');
                if (eq < 0) continue;

                string name = decl[..eq].Trim();
                if (name.Contains('[')) continue; /// arrays unsupported

                if (TryParseGlslValue(type, decl[(eq + 1)..].Trim(), out object? value))
                    defaults[name] = value!;
                else
                    Log.log($"Shader '{Name}': can't parse default for '{name}' ({type}): {decl[(eq + 1)..].Trim()}", LogType.warning);
            }
        }
        return defaults;
    }
    /// Splits on commas that are not inside (), [] -- so "vec3(1,2,3), b = 2" splits correctly.
    static List<string> SplitTopLevel (string s) {
        List<string> parts = new();
        int depth = 0, start = 0;
        for (int i = 0; i < s.Length; i++) {
            char c = s[i];
            if (c == '(' || c == '[') depth++;
            else if (c == ')' || c == ']') depth--;
            else if (c == ',' && depth == 0) {
                parts.Add(s[start..i].Trim());
                start = i + 1;
            }
        }
        parts.Add(s[start..].Trim());
        return parts;
    }

    static bool TryParseFloat (string s, out float f) {
        s = s.Trim().TrimEnd('f', 'F');
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
    }

    static bool TryParseGlslValue (string type, string expr, out object? value) {
        value = null;
        switch (type) {
            case "float":
                if (TryParseFloat(expr, out float f)) { value = f; return true; }
                return false;
            case "int":
                if (int.TryParse(expr, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int i)) { value = i; return true; }
                return false;
            case "uint":
                if (uint.TryParse(expr.TrimEnd('u', 'U'), NumberStyles.None, CultureInfo.InvariantCulture, out uint u)) { value = u; return true; }
                return false;
            case "bool":
                if (bool.TryParse(expr, out bool b)) { value = b; return true; }
                return false;
            case "vec2":
            case "vec3":
            case "vec4":
                return TryParseVec(type, expr, out value);
        }
        return false;
    }

    static bool TryParseVec (string type, string expr, out object? value) {
        value = null;
        int n = type[3] - '0';

        if (!expr.StartsWith(type) || !expr.EndsWith(')')) return false;
        int open = expr.IndexOf('(');
        if (open < 0) return false;

        List<string> args = SplitTopLevel(expr[(open + 1)..^1]);
        float[] v = new float[n];

        if (args.Count == 1) {                       /// vec3(0.5) -> broadcast
            if (!TryParseFloat(args[0], out float s)) return false;
            Array.Fill(v, s);
        } else if (args.Count == n) {
            for (int k = 0; k < n; k++)
                if (!TryParseFloat(args[k], out v[k])) return false;
        } else return false;

        value = n switch {
            2 => new Vector2(v[0], v[1]),
            3 => new Vector3(v[0], v[1], v[2]),
            _ => new Vector4(v[0], v[1], v[2], v[3]),
        };
        return true;
    }

    /// Reads back every active uniform from the linked program.
    /// Call once right after linking, so Material can look up what a shader actually needs.
    void ReflectUniforms () {
        ActiveUniforms.Clear();
        GL.GetProgram(_program, GLEnum.ActiveUniforms, out int count);

        for (uint i = 0; i < count; i++) {
            string name = GL.GetActiveUniform(_program, i, out int size, out UniformType type);
            uint index = i;
            int blockIndex;
            unsafe {
                GL.GetActiveUniforms(_program, 1, &index, GLEnum.UniformBlockIndex, &blockIndex);
            }
            if (blockIndex != -1) continue; /// belongs to a UBO -- not material-owned, skip

            if (name.Contains('[') && !name.EndsWith("[0]")) continue;
            string key = name.Contains('[') ? name[..name.IndexOf('[')] : name;
            ActiveUniforms[key] = new UniformInfo { Name = key, Type = type, Size = size };
        }
    }

    public void SetInt (string name, int value) {
        int location = GetLocation(name);
        GL.Uniform1(location, value);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetInt)} {err}", LogType.warning);
    }
    public void SetFloat (string name, float value) {
        int location = GetLocation(name);
        GL.Uniform1(location, value);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetFloat)} {err}", LogType.warning);
    }
    public void SetFloatArray (string name, float[] values) {
        int location = GetLocation(name);
        GL.Uniform1(location, (uint)values.Length, values);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetFloatArray)} {err}", LogType.warning);
    }

    public void SetVector2 (string name, float x, float y) {
        int location = GetLocation(name);
        GL.Uniform2(location, x, y);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector2)} {err}", LogType.warning);
    }
    public void SetVector2 (string name, Vector2 vec2) {
        int location = GetLocation(name);
        GL.Uniform2(location, vec2.X, vec2.Y);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector2)} {err}", LogType.warning);
    }

    public void SetVector3 (string name, float x, float y, float z) {
        int location = GetLocation(name);
        GL.Uniform3(location, x, y, z);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector3)} {err}", LogType.warning);
    }
    public void SetVector3 (string name, Vector3 vec3) {
        int location = GetLocation(name);
        GL.Uniform3(location, vec3.X, vec3.Y, vec3.Z);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector3)} {err}", LogType.warning);
    }
    public void SetVector3Array (string name, Vector3[] values) {
        int location = GetLocation(name);
        GL.Uniform3(location, (uint)values.Length, ref values[0].X);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector3Array)} {err}", LogType.warning);
    }

    public void SetVector4 (string name, float x, float y, float z, float w) {
        int location = GetLocation(name);
        GL.Uniform4(location, x, y, z, w);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector4)} {err}", LogType.warning);
    }
    public void SetVector4 (string name, Vector4 vec4) {
        int location = GetLocation(name);
        GL.Uniform4(location, vec4.X, vec4.Y, vec4.Z, vec4.W);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector4)} {err}", LogType.warning);
    }

    public void SetBool (string name, bool value) {
        int location = GetLocation(name);
        GL.Uniform1(location, value ? 1 : 0);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetBool)} {err}", LogType.warning);
    }

    /*public void SetMatrix4 (string name, float[] matrix) {
        int location = GL.GetUniformLocation(_program, name);
        unsafe {
            fixed (float* ptr = matrix) {
                GL.UniformMatrix4(location, 1, false, ptr);
            }
        }
        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetMatrix4)} {err}", LogType.warning);
    }*/
    public void SetMatrix4x4 (string name, Matrix4x4 matrix) {
        int location = GetLocation(name); ;
        //if (location == -1) {
        //    //string message = $"Uniform '{name}' not found in program {Name}!";
        //    //Log.log(message, LogType.warning);
        //    //throw new Exception(message);
        //}
        unsafe {
            GL.UniformMatrix4(location, 1, false, (float*)&matrix);
        }

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetMatrix4x4)} {err}", LogType.warning);
    }

    public void SetTexture (string name, Texture texture) {
        if (!_textureUnits.TryGetValue(name, out int unitIndex)) {
            if (name == Shader.uSkybox) {
                unitIndex = 0; /// reserved slot — always 0, never auto-assigned to anything else
            } else {
                unitIndex = nextTextureUnit++;
                const int maxUnits = 16; /// GL_MAX_TEXTURE_IMAGE_UNITS guaranteed minimum across all GL 3.3+ hardware
                if (unitIndex >= maxUnits)
                    throw new Exception($"Shader '{Name}': ran out of texture units assigning '{name}' (unit {unitIndex} >= {maxUnits}).");
            }
            _textureUnits[name] = unitIndex;
        }

        TextureUnit unit = TextureUnit.Texture0 + unitIndex;
        texture.Bind(unit);

        int location = GetLocation(name);
        GL.Uniform1(location, unitIndex);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetTexture)} {err}", LogType.warning);
    }

    private int GetLocation (string name) {
        if (!_locations.TryGetValue(name, out int location)) {
            location = GL.GetUniformLocation(_program, name);
            _locations[name] = location;
        }
        return location;
    }



    public void Save (string path) {
        Path = path;
        Json.Write(path, this);
    }

    public static Shader? Load (string path, int part = 100) {
        Shader? shader = Json.Read<Shader>(path);
        return shader;
    }


    public void Dispose () {
        GL.DeleteProgram(_program);
    }

}
