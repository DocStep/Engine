using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Shader : IDisposable {
    public Shader (string vertexSource, string fragmentSource, string name = "unnamed", bool isLit = true) {
        GL = Renderer.GL;
        Name = name;
        this.isLit = isLit;

        _vertexSource = vertexSource;
        _fragmentSource = fragmentSource;


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
    /*public Shader (Shader shader) : this(shader._vertexSource, shader._fragmentSource, shader.Name + " (copy)") {
        pass = shader.pass;
        depthTest = shader.depthTest;
        depthWrite = shader.depthWrite;
    }*/

    private readonly GL GL;
    private readonly uint _program;
    public readonly string Name = "Unnamed";
    private readonly string _vertexSource;
    private readonly string _fragmentSource;
    public bool isLit;

    public static RendererGLStats Stats = default;
    /*public static void StatsReset () {
        Stats = new RendererGLStats();
    }*/


    public const string View = "uView";
    public const string Projection = "uProjection";
    public const string InvProjection = "uInvProjection";
    public const string ViewPos = "uViewPos";
    public const string Model = "uModel";
    public const string NormalMatrix = "uNormalMatrix";
    public const string CameraPos = "uCameraPos";
    public const string Scene = "uSceneColor";
    public const string Depth = "uDepth";

    public const string SunLightCount = "uSunLightCount";
    public const string SunLightDir = "uSunLightDir";
    public const string SunLightColor = "uSunLightColor";
    public const string SunLightIntensity = "uSunLightIntensity";

    public const string PointLightCount = "uPointLightCount";
    public const string PointLightColor = "uPointLightColor";
    public const string PointLightIntensity = "uPointLightIntensity";
    public const string PointLightPos = "uPointLightPos";
    public const string PointLightRange = "uPointLightRange";

    public const string Exposure = "uExposure";
    public const string AmbientColor = "uAmbientColor";
    public const string AmbientColorIntensity = "uAmbientColorIntensity";
    public const string ReflectionIntensity = "uReflectionIntensity";


    public const string MaxReflectionLod = "uMaxReflectionLod";
    public const string Skybox = "uSkybox";

    public const string Color = "uColor";
    public const string Texture = "uTexture";
    public const string Smoothness = "uSmoothness";
    public const string Metallic = "uMetallic";
    public const string Alpha = "uAlpha";
    public const string Radius = "uRadius";
    public const string Fade = "uFade";
    public const string Tint = "uTint";



    public void Use () {
        GL.UseProgram(_program);
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

    public void SetTexture (string name, TextureUnit unit) {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        //var err = GL.GetError();
        //if (err != GLEnum.NoError) Log.log($"GL error {nameof(SetTexture)} {err}", LogType.warning);
    }



    public void Dispose () {
        GL.DeleteProgram(_program);
    }

}
