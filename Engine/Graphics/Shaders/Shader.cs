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

        uint vertex;
        uint fragment;
        try {
            vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        } catch (Exception ex) {
            throw new Exception($"Failed to compile Shader_Vertex {VertexSourcePath}", ex);
        }
        try {
            fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        } catch (Exception ex) {
            throw new Exception($"Failed to compile Shader_Fragment {VertexSourcePath}", ex);
        }

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
        ReflectUniforms();

        GL.DeleteProgram(oldProgram);
    }
    uint CompileShader (ShaderType type, string source) {
        uint shaderId = GL.CreateShader(type);
        GL.ShaderSource(shaderId, source);
        GL.CompileShader(shaderId);

        GL.GetShader(shaderId, ShaderParameterName.CompileStatus, out int status);
        if (status == 0) {
            string log = GL.GetShaderInfoLog(shaderId);
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

    [JsonIgnore, Hide] private readonly GL GL;
    [JsonIgnore, Hide] private uint _program;
    [JsonIgnore, Hide] private readonly Dictionary<string, int> _textureUnits = new();
    [JsonIgnore, Hide] private int _nextTextureUnit = 1; /// 0 is reserved for uSkybox, bound directly in SetSceneUniformsSkybox

    [JsonIgnore, Hide] static int _nextId = 0;
    [JsonIgnore, Hide] public readonly int Id_Renderer = System.Threading.Interlocked.Increment(ref _nextId);

    [JsonIgnore, Hide]
    public Dictionary<string, UniformInfo> ActiveUniforms { get; private set; } = new Dictionary<string, UniformInfo>();

    /// Uniform names the renderer sets globally per-frame/per-pass (camera, lights, time...).
    /// FillDefaults must never touch these -- they don't belong to any one material.
    [JsonIgnore, Hide]
    public static readonly HashSet<string> ReservedUniforms = new() {
        View, Projection, ViewPos, Model, NormalMatrix, CameraPos, Scene, Depth,
        SunLightCount, SunLightDir, SunLightColor, SunLightIntensity,
        PointLightCount, PointLightColor, PointLightIntensity, PointLightPos, PointLightRange,
        Skybox, MaxReflectionLod,
        Exposure, AmbientColor, AmbientColorIntensity, ReflectionIntensity,
        SHAr, SHAg, SHAb, SHBr, SHBg, SHBb, SHC,
    };

    [JsonIgnore, Hide]
    public readonly static Dictionary<UniformType, object> UniformTypeDefaults = new Dictionary<UniformType, object>() {
        [UniformType.Int] = 0,
        [UniformType.Float] = 0.5f,
        [UniformType.FloatVec2] = Vector2.Zero,
        [UniformType.FloatVec3] = Vector3.One,
        [UniformType.FloatVec4] = Vector4.One,
        //[UniformType.Sampler2D] = Graphics.Texture.White,
    };
    [JsonIgnore, Hide]
    public readonly static Dictionary<string, object> UniformDefaults = new Dictionary<string, object>() {
        [Shader.Color] = Vector3.One,
        [Shader.Alpha] = 1f,
        [Shader.Smoothness] = 0.5f,
        [Shader.Metallic] = 0f,
        [Shader.ReflectionIntensity] = 1f,
    };

    [JsonIgnore] public static RendererGLStats Stats = default;


    [JsonIgnore, Hide] public const int SkyboxUnitIndex = 0;
    [JsonIgnore, Hide] public const int TextureUnitIndex = 10;


    [JsonIgnore, Hide] public const string View = "uView";
    [JsonIgnore, Hide] public const string Projection = "uProjection";
    [JsonIgnore, Hide] public const string InvProjection = "uInvProjection";
    [JsonIgnore, Hide] public const string ViewPos = "uViewPos";
    [JsonIgnore, Hide] public const string Model = "uModel";
    [JsonIgnore, Hide] public const string NormalMatrix = "uNormalMatrix";
    [JsonIgnore, Hide] public const string CameraPos = "uCameraPos";
    [JsonIgnore, Hide] public const string Scene = "uSceneColor";
    [JsonIgnore, Hide] public const string Depth = "uDepth";

    [JsonIgnore, Hide] public const string SunLightCount = "uSunLightCount";
    [JsonIgnore, Hide] public const string SunLightDir = "uSunLightDir";
    [JsonIgnore, Hide] public const string SunLightColor = "uSunLightColor";
    [JsonIgnore, Hide] public const string SunLightIntensity = "uSunLightIntensity";

    [JsonIgnore, Hide] public const string PointLightCount = "uPointLightCount";
    [JsonIgnore, Hide] public const string PointLightColor = "uPointLightColor";
    [JsonIgnore, Hide] public const string PointLightIntensity = "uPointLightIntensity";
    [JsonIgnore, Hide] public const string PointLightPos = "uPointLightPos";
    [JsonIgnore, Hide] public const string PointLightRange = "uPointLightRange";

    [JsonIgnore, Hide] public const string Skybox = "uSkybox";
    [JsonIgnore, Hide] public const string MaxReflectionLod = "uMaxReflectionLod";

    [JsonIgnore, Hide] public const string Exposure = "uExposure";
    [JsonIgnore, Hide] public const string AmbientColor = "uAmbientColor";
    [JsonIgnore, Hide] public const string AmbientColorIntensity = "uAmbientColorIntensity";
    [JsonIgnore, Hide] public const string ReflectionIntensity = "uReflectionIntensity";

    [JsonIgnore, Hide] public const string SHAr = "uSHAr";
    [JsonIgnore, Hide] public const string SHAg = "uSHAg";
    [JsonIgnore, Hide] public const string SHAb = "uSHAb";
    [JsonIgnore, Hide] public const string SHBr = "uSHBr";
    [JsonIgnore, Hide] public const string SHBg = "uSHBg";
    [JsonIgnore, Hide] public const string SHBb = "uSHBb";
    [JsonIgnore, Hide] public const string SHC = "uSHC";

    [JsonIgnore, Hide] public const string Color = "uColor";
    [JsonIgnore, Hide] public const string Texture = "uTexture";
    [JsonIgnore, Hide] public const string HasTexture = "uHasTexture";
    [JsonIgnore, Hide] public const string Smoothness = "uSmoothness";
    [JsonIgnore, Hide] public const string Metallic = "uMetallic";
    [JsonIgnore, Hide] public const string Alpha = "uAlpha";
    [JsonIgnore, Hide] public const string Radius = "uRadius";
    [JsonIgnore, Hide] public const string Fade = "uFade";
    [JsonIgnore, Hide] public const string Tint = "uTint";



    public void Use () {
        GL.UseProgram(_program);
        //_nextTextureUnit = 0;
        //GLEnum err = GL.GetError();
        //if (err != GLEnum.NoError) 
        //    Console.WriteLine($"UseProgram({_program}, {Name}) Error: {err}");
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
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform1(location, value);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetInt)} {err}", LogType.warning);
    }
    public void SetFloat (string name, float value) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform1(location, value);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetFloat)} {err}", LogType.warning);
    }
    public void SetFloatArray (string name, float[] values) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform1(location, (uint)values.Length, values);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetFloatArray)} {err}", LogType.warning);
    }

    public void SetVector2 (string name, float x, float y) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform2(location, x, y);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector2)} {err}", LogType.warning);
    }
    public void SetVector2 (string name, Vector2 vec2) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform2(location, vec2.X, vec2.Y);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector2)} {err}", LogType.warning);
    }

    public void SetVector3 (string name, float x, float y, float z) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform3(location, x, y, z);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector3)} {err}", LogType.warning);
    }
    public void SetVector3 (string name, Vector3 vec3) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform3(location, vec3.X, vec3.Y, vec3.Z);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector3)} {err}", LogType.warning);
    }
    public void SetVector3Array (string name, Vector3[] values) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform3(location, (uint)values.Length, ref values[0].X);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector3Array)} {err}", LogType.warning);
    }

    public void SetVector4 (string name, float x, float y, float z, float w) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform4(location, x, y, z, w);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector4)} {err}", LogType.warning);
    }
    public void SetVector4 (string name, Vector4 vec4) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform4(location, vec4.X, vec4.Y, vec4.Z, vec4.W);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetVector4)} {err}", LogType.warning);
    }

    public void SetBool (string name, bool value) {
        int location = GL.GetUniformLocation(_program, name);
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
        int location = GL.GetUniformLocation(_program, name);
        if (location == -1) {
            string message = $"Uniform '{name}' not found in program {Name}!";
            //Log.log(message, LogType.warning);
            //throw new Exception(message);
        }
        unsafe {
            GL.UniformMatrix4(location, 1, false, (float*)&matrix);
        }

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetMatrix4x4)} {err}", LogType.warning);
    }

    public void SetTexture (string name, Texture texture) {
        int unitIndex = name switch {
            Shader.Skybox => SkyboxUnitIndex,
            Shader.Texture => TextureUnitIndex,
            _ => throw new Exception($"No texture unit assigned for uniform '{name}' — add it to TextureUnits."),
        };

        TextureUnit unit = TextureUnit.Texture0 + unitIndex;
        texture.Bind(unit);

        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform1(location, unitIndex);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetTexture)} {err}", LogType.warning);
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
