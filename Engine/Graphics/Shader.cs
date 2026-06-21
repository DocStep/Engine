using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace Engine.Graphics;


public class Shader : IDisposable {
    private readonly GL _gl;
    private readonly uint _handle;

    public Shader (GL gl, string vertexSource, string fragmentSource) {
        _gl = gl;

        uint vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, vertex);
        _gl.AttachShader(_handle, fragment);
        _gl.LinkProgram(_handle);

        _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0) {
            string log = _gl.GetProgramInfoLog(_handle);
            throw new Exception($"Shader program failed to link: {log}");
        }

        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
    }

    private uint CompileShader (ShaderType type, string source) {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0) {
            string log = _gl.GetShaderInfoLog(shader);
            throw new Exception($"{type} failed to compile: {log}");
        }

        return shader;
    }

    public void Use () {
        _gl.UseProgram(_handle);
    }

    public void SetMatrix4 (string name, float[] matrix) {
        int location = _gl.GetUniformLocation(_handle, name);
        unsafe {
            fixed (float* ptr = matrix) {
                _gl.UniformMatrix4(location, 1, false, ptr);
            }
        }
    }

    public void SetVector3 (string name, float x, float y, float z) {
        int location = _gl.GetUniformLocation(_handle, name);
        _gl.Uniform3(location, x, y, z);
    }
    public void SetVector3 (string name, Vector3D<float> vec3) {
        int location = _gl.GetUniformLocation(_handle, name);
        _gl.Uniform3(location, vec3.X, vec3.Y, vec3.Z);
    }

    public void SetColor (string name, Vector3D<float> color) {
        int location = _gl.GetUniformLocation(_handle, name);
        _gl.Uniform3(location, color.X, color.Y, color.Z);
    }


    public void SetFloat (string name, float value) {
        int location = _gl.GetUniformLocation(_handle, name);
        _gl.Uniform1(location, value);
    }

    public void Dispose () {
        _gl.DeleteProgram(_handle);
    }
}