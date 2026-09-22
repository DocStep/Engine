using Silk.NET.OpenGL;
using Newtonsoft.Json;

namespace Engine.Graphics;


public class Shader : IAsset<Shader> {
    public Shader (string vertexSourcePath, string fragmentSourcePath, string name = "unnamed", bool isLit = true) {
        GL = Renderer.GL;
        Name = name;
        this.isLit = isLit;

        this.vertexSourcePath = vertexSourcePath;
        this.fragmentSourcePath = fragmentSourcePath;

        string vertexSource = Assets.LoadText(vertexSourcePath);
        string fragmentSource = Assets.LoadText(fragmentSourcePath);
        uint vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _program = GL.CreateProgram();
        GL.AttachShader(_program, vertex);
        GL.AttachShader(_program, fragment);
        GL.LinkProgram(_program);
        GL.GetProgram(_program, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
        //for (uint i = 0; i < uniformCount; i++) {
        //    GL.GetActiveUniform(_program, i, 256, out uint length, out int size, out UniformType type, out string namee);
        //    if (namee.Contains("Sun")) Log.log($"uniform {namee}", $"size={size} type={type}");
        //}

        GL.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int status);
        Stats.RecordCompile(status);
        if (status == 0) {
            string log = GL.GetProgramInfoLog(_program);
            throw new Exception($"Shader program failed to link: {log}");
        }

        GL.DetachShader(_program, vertex);
        GL.DetachShader(_program, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

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
    }
    public void Compile () {

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

    [JsonProperty] private readonly string vertexSourcePath;
    [JsonProperty] private readonly string fragmentSourcePath;
    public bool isLit;

    [JsonIgnore] private readonly GL GL;
    [JsonIgnore] private readonly uint _program;
    [JsonIgnore] private int _nextTextureUnit = 0;

    [JsonIgnore] public static RendererGLStats Stats = default;
    /*public static void StatsReset () {
        Stats = new RendererGLStats();
    }*/


    [JsonIgnore] public const string View = "uView";
    [JsonIgnore] public const string Projection = "uProjection";
    [JsonIgnore] public const string InvProjection = "uInvProjection";
    [JsonIgnore] public const string ViewPos = "uViewPos";
    [JsonIgnore] public const string Model = "uModel";
    [JsonIgnore] public const string NormalMatrix = "uNormalMatrix";
    [JsonIgnore] public const string CameraPos = "uCameraPos";
    [JsonIgnore] public const string Scene = "uSceneColor";
    [JsonIgnore] public const string Depth = "uDepth";

    [JsonIgnore] public const string SunLightCount = "uSunLightCount";
    [JsonIgnore] public const string SunLightDir = "uSunLightDir";
    [JsonIgnore] public const string SunLightColor = "uSunLightColor";
    [JsonIgnore] public const string SunLightIntensity = "uSunLightIntensity";

    [JsonIgnore] public const string PointLightCount = "uPointLightCount";
    [JsonIgnore] public const string PointLightColor = "uPointLightColor";
    [JsonIgnore] public const string PointLightIntensity = "uPointLightIntensity";
    [JsonIgnore] public const string PointLightPos = "uPointLightPos";
    [JsonIgnore] public const string PointLightRange = "uPointLightRange";

    [JsonIgnore] public const string Exposure = "uExposure";
    [JsonIgnore] public const string AmbientColor = "uAmbientColor";
    [JsonIgnore] public const string AmbientColorIntensity = "uAmbientColorIntensity";
    [JsonIgnore] public const string ReflectionIntensity = "uReflectionIntensity";


    [JsonIgnore] public const string MaxReflectionLod = "uMaxReflectionLod";
    [JsonIgnore] public const string Skybox = "uSkybox";

    [JsonIgnore] public const string Color = "uColor";
    [JsonIgnore] public const string Texture = "uTexture";
    [JsonIgnore] public const string Smoothness = "uSmoothness";
    [JsonIgnore] public const string Metallic = "uMetallic";
    [JsonIgnore] public const string Alpha = "uAlpha";
    [JsonIgnore] public const string Radius = "uRadius";
    [JsonIgnore] public const string Fade = "uFade";
    [JsonIgnore] public const string Tint = "uTint";



    public void Use () {
        GL.UseProgram(_program);
        _nextTextureUnit = 0;
        //GLEnum err = GL.GetError();
        //if (err != GLEnum.NoError) 
        //    Console.WriteLine($"UseProgram({_program}, {Name}) Error: {err}");
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
        TextureUnit unit = TextureUnit.Texture0 + _nextTextureUnit;
        texture.Bind(unit);

        int location = GL.GetUniformLocation(texture.Handle, name);
        GL.Uniform1(location, _nextTextureUnit);

        _nextTextureUnit++;

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetTexture)} {err}", LogType.warning);
    }



    public void Save (string path) {
        Path = path;
        Json.Write(path, this);
    }

    public static Shader? Load (string path) {
        Shader shader = Json.Read<Shader>(path);
        return shader;
    }


    public void Dispose () {
        GL.DeleteProgram(_program);
    }

}
