using Silk.NET.OpenGL;

namespace Engine.Graphics;


public static class GLDebug {
    public static void Init () {
        GL = Renderer.GL;

        _lineVAO = GL.GenVertexArray();
        _lineVBO = GL.GenBuffer();

        GL.BindVertexArray(_lineVAO);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _lineVBO);

        unsafe {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float)*6), null, BufferUsageARB.DynamicDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3*sizeof(float), (void*)0);
        }
        
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    private static GL GL = null!;
    private static uint _lineVAO;
    private static uint _lineVBO;


    private static List<DebugLine> Lines = new List<DebugLine>();
    //public static void Clear () => Lines.Clear();


    public static void Line (Vector3 pos1, Vector3 pos2, Vector3 color) {
        Lines.Add(new DebugLine() { Position1 = pos1, Position2 = pos2, Color = color });
    }


    public static void DrawAll () {
        GL.Disable(EnableCap.DepthTest);

        //Renderer.Instance.SetSceneUniformsUnlit(AssetsEngine._sh_Unlit);
        int count = Lines.Count;
        for (int i = 0; i < count; i++) {
            DrawLine(Lines[i]);
        }
        Lines.Clear();
    }
    public unsafe static void DrawLine (DebugLine debugLine) {
        Span<float> verts = stackalloc float[6] {
            debugLine.Position1.X, debugLine.Position1.Y, debugLine.Position1.Z,
            debugLine.Position2.X, debugLine.Position2.Y, debugLine.Position2.Z
        };

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _lineVBO);
        fixed (float* p = verts) {
            GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(sizeof(float)*6), p);
        }

        Matrix4x4 mesh_m4x4 = Matrix4x4.Identity;
        Shader unlit = AssetsEngine._sh_Unlit;
        unlit.SetMatrix4(Shader.Model, Matrix4x4.ToArray(mesh_m4x4));
        unlit.SetVector3(Shader.Color, debugLine.Color);

        GL.BindVertexArray(_lineVAO);
        GL.DrawArrays(PrimitiveType.Lines, 0, 2);
        GL.BindVertexArray(0);
    }

}


public struct DebugLine {
    public Vector3 Position1;
    public Vector3 Position2;
    public Vector3 Color;
}
