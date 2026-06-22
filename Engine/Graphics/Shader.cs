using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace Engine.Graphics;


public class Shader : IDisposable {
    private readonly GL GL;
    private readonly uint _program;
    public string Name = "Unnamed";

    public Shader (string vertexSource, string fragmentSource, string name = "unnamed") {
        GL = Renderer.Instance.GL;
        Name = name;

        uint vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _program = GL.CreateProgram();
        GL.AttachShader(_program, vertex);
        GL.AttachShader(_program, fragment);
        GL.LinkProgram(_program);

        GL.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0) {
            string log = GL.GetProgramInfoLog(_program);
            throw new Exception($"Shader program failed to link: {log}");
        }

        GL.DetachShader(_program, vertex);
        GL.DetachShader(_program, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
    }

    private uint CompileShader (ShaderType type, string source) {
        uint shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0) {
            string log = GL.GetShaderInfoLog(shader);
            throw new Exception($"{type} failed to compile: {log}");
        }

        return shader;
    }

    public void Use () {
        GL.UseProgram(_program);
        //var err = GL.GetError();
        //if (err != GLEnum.NoError) 
        //    Console.WriteLine($"UseProgram({_program}, {Name}) Error: {err}");
    }

    public void SetMatrix4 (string name, float[] matrix) {
        int location = GL.GetUniformLocation(_program, name);
        unsafe {
            fixed (float* ptr = matrix) {
                GL.UniformMatrix4(location, 1, false, ptr);
            }
        }
    }
    public void SetMatrix4X4 (string name, Matrix4X4<float> matrix) {
        int location = GL.GetUniformLocation(_program, name);
        if (location == -1) Console.WriteLine($"Uniform '{name}' not found in program {_program}!");
        unsafe {
            GL.UniformMatrix4(location, 1, false, (float*)&matrix);
        }
    }

    private void SetTexture (string name, TextureUnit unit) {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void SetVector3 (string name, float x, float y, float z) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform3(location, x, y, z);
    }
    public void SetVector3 (string name, Vector3D<float> vec3) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform3(location, vec3.X, vec3.Y, vec3.Z);
    }

    public void SetColor (string name, Vector3D<float> color) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform3(location, color.X, color.Y, color.Z);
    }

    public void SetInt (string name, int value) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform1(location, value);
    }
    public void SetFloat (string name, float value) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform1(location, value);
    }

    public void SetBool (string name, bool value) {
        int location = GL.GetUniformLocation(_program, name);
        GL.Uniform1(location, value ? 1 : 0);
    }

    public void Dispose () {
        GL.DeleteProgram(_program);
    }
}