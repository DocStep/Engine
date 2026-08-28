using Silk.NET.OpenGL;

namespace Editor.Graphics.UI;


public class RectGizmo : IDisposable {
    public RectGizmo () {
        GL = Engine.Graphics.Renderer.GL;
        _material = new Engine.Graphics.Material(AssetsEngine._mat_Unlit);

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        uint stride = (uint)(2*sizeof(float));
        GL.EnableVertexAttribArray(0);
        unsafe {
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        }
    }

    GL GL;
    uint _vao, _vbo;
    Engine.Graphics.Material _material;
    Vector3 color = new Vector3(0f, 1f, 1f);


    public void Draw (Engine.Graphics.UI.RectTransform rect, int targetWidth, int targetHeight) {
        Vector2 min = rect.Min;
        Vector2 max = rect.Max;
        Matrix4x4 rectMatrix = rect.RectMatrix;

        Vector2[] corners = {
            TransformPoint(new Vector2(min.X, min.Y), rectMatrix),
            TransformPoint(new Vector2(max.X, min.Y), rectMatrix),
            TransformPoint(new Vector2(max.X, max.Y), rectMatrix),
            TransformPoint(new Vector2(min.X, max.Y), rectMatrix),
        };

        GL.Viewport(0, 0, (uint)targetWidth, (uint)targetHeight);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);

        _material.shader.Use();
        _material.shader.SetMatrix4x4(Engine.Graphics.Shader.Projection, Engine.Graphics.Renderer.Instance.m4x4_ProjectionUI);
        _material.shader.SetMatrix4x4(Engine.Graphics.Shader.View, Matrix4x4.Identity);
        _material.shader.SetMatrix4x4(Engine.Graphics.Shader.Model, Matrix4x4.Identity);
        _material.shader.SetVector3(Engine.Graphics.Shader.Color, color);
        _material.shader.SetFloat(Engine.Graphics.Shader.Alpha, 1);

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        unsafe {
            fixed (Vector2* ptr = corners) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(corners.Length*sizeof(Vector2)), ptr, BufferUsageARB.StreamDraw);
            }
        }

        GL.DrawArrays(PrimitiveType.LineLoop, 0, (uint)corners.Length);

        GL.Enable(EnableCap.DepthTest);
    }

    private static Vector2 TransformPoint (Vector2 point, Matrix4x4 matrix) {
        Vector3 result = Vector3.Transform(new Vector3(point, 0f), matrix);
        return new Vector2(result.X, result.Y);
    }


    public void Dispose () {
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
    }

}