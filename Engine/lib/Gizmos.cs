using Silk.NET.OpenGL;
using Engine.Graphics;
using static Engine.AssetsEngine;
using static Engine.Graphics.Shader;

namespace Engine;


public static class Gizmos {
    static Gizmos () {
        //Renderer.Instance.de_GizmosDraw += DrawGizmos;
        GL = Renderer.Instance.GL;
    }

    static GL GL = null!;

    private static float cameraOrbitCenterRadius = 0.5f;


    public static void DrawGizmos () {
        DrawGizmoGrid();
        DrawGizmoAxes();
        if (Constants.drawGizmosSun) DrawGizmoSun();

        GLDebug.DrawAll();

        DrawGizmoCameraOrbitCenter();

        Renderer.Instance.de_GizmosDraw?.Invoke();

        //_gizmo_Selected.Draw();

        /// UI Layer
        DrawGizmoAxesWidget();
    }
    private static void DrawGizmoGrid () {
        GL.DepthMask(false);

        GL.DepthRange(0.0001, 1.0);

        _sh_Grid.Use();
        Renderer.SetSceneUniformsLit(_sh_Grid);
        _sh_Grid.SetMatrix4(Model, Renderer._uModelIdentity);
        _sh_Grid.SetVector3(CameraPos, Camera.Instance.cameraPos);
        _sh_Grid.SetVector3(Color, Constants.lightGray);
        _sh_Grid.SetFloat(Alpha, 0.5f);
        _sh_Grid.SetFloat(Radius, 200f);
        _sh_Grid.SetFloat(Fade, 50f);
        _mesh_GridWireframe.Draw(PrimitiveType.Lines);

        GL.DepthRange(0.0, 1.0);
        GL.DepthMask(true);
    }
    private static void DrawGizmoAxes () {
        _sh_Axes.Use();
        Renderer.SetSceneUniformsLit(_sh_Axes);
        _sh_Axes.SetMatrix4(Model, Renderer._uModelIdentity);
        _sh_Axes.SetVector3(CameraPos, Camera.Instance.cameraPos);
        _sh_Axes.SetFloat(Alpha, 0.5f);
        _sh_Axes.SetFloat(Radius, 200f);
        _sh_Axes.SetFloat(Fade, 50f);
        _mesh_AxesWireframe.Draw(PrimitiveType.Lines);
    }
    private static void DrawGizmoAxesWidget () {
        const int gizmoSize = 90;
        const int gizmoMargin = 16;

        int windowWidth = Engine.Window.Size.X;
        int windowHeight = Engine.Window.Size.Y;

        int gizmoX = windowWidth - gizmoSize - gizmoMargin;
        int gizmoY = windowHeight - gizmoSize - gizmoMargin;

        GL.Disable(EnableCap.DepthTest);
        GL.Viewport(gizmoX, gizmoY, (uint)gizmoSize, (uint)gizmoSize);
        GL.Clear(ClearBufferMask.DepthBufferBit);

        Matrix4x4 rotation = Camera.Instance.cameraRot;
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, rotation);
        Vector3 gizmoCamPos = -forward*5f;
        Matrix4x4 gizmoView = Matrix4x4.CreateLookAtLeftHanded(gizmoCamPos, Vector3.Zero, up);
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        Matrix4x4 gizmoProjection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            Constants._cameraFOV, aspect, Constants._cameraPlaneClose, Constants._cameraPlaneFar);

        _sh_Axes.Use();
        _sh_Axes.SetMatrix4(View, Matrix4x4.ToArray(gizmoView));
        _sh_Axes.SetMatrix4(Projection, Matrix4x4.ToArray(gizmoProjection));
        _sh_Axes.SetMatrix4(Model, Matrix4x4.ToArray(Matrix4x4.CreateScale(0.002f)));

        _mesh_AxesWireframe.Draw(PrimitiveType.Lines);

        GL.Enable(EnableCap.DepthTest);
        GL.Viewport(Engine.Window.Size);
    }
    private static void DrawGizmoCameraOrbitCenter () {
        if (CameraEditor.Instance is null) return;

        GL.DepthMask(false);

        _sh_Unlit.Use();
        Renderer.SetSceneUniformsLit(_sh_Unlit);
        float dist = (CameraEditor.Instance.cameraOrbitCenterPos - Camera.Instance.cameraPos).Length();
        Matrix4x4 gizmoSphereModel = Matrix4x4.CreateScale(cameraOrbitCenterRadius*dist*0.01f)
            *Matrix4x4.CreateTranslation(CameraEditor.Instance.cameraOrbitCenterPos);
        _sh_Unlit.SetMatrix4(Model, Matrix4x4.ToArray(gizmoSphereModel));
        _sh_Unlit.SetVector3(Color, 0.5f, 0.5f, 0.5f);
        _sh_Unlit.SetFloat(Alpha, 0.2f);
        _mesh_Sphere.Draw();

        GL.DepthMask(true);
    }
    private static void DrawGizmoSun () {
        GL.Disable(EnableCap.CullFace);

        _sh_Unlit.Use();
        Renderer.SetSceneUniformsLit(_sh_Unlit);
        //Matrix4x4 mesh_m4x4 = Utils.RotationFromDirection(Constants.sunLightDir)*Matrix4x4.Position(0f, 5f, 0f);
        Matrix4x4 mesh_m4x4 = Matrix4x4.RotationFromDirection(Constants.sunLightDir)*Matrix4x4.Position(0f, 5f, 0f);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
        _sh_Unlit.SetMatrix4(Model, mesh_uModel);
        _sh_Unlit.SetColor(Color, Constants.yellow);
        _sh_Unlit.SetFloat(Alpha, 0.5f);
        if (Constants._drawArrowAsMesh) _mesh_Arrow3D.Draw();
        else _mesh_ArrowWireframe.Draw(PrimitiveType.Lines);

        GL.Enable(EnableCap.CullFace);
    }


    public static void DrawMaterialsGrid (float offsetX, float offsetZ, int testGridCount, float testGridDensity) {
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        Renderer.SetSceneUniformsLit(_sh_Lit);
        _sh_Lit.SetColor(Color, Constants.black);
        for (int x = 0; x < testGridCount*testGridDensity; x++) {
            for (int z = 0; z < testGridCount*testGridDensity; z++) {
                Renderer.Instance.AddRenderInfo(new RenderInfo() {
                    pos = new Vector3(2f*x/testGridDensity + offsetX, 0f, -2f*z/testGridDensity + offsetZ),
                    mesh = AssetsEngine._mesh_Sphere,
                    shader = AssetsEngine._sh_Lit,
                    material = AssetsEngine._mat_Smooth,
                });

                Matrix4x4 mesh_m4x4 = Matrix4x4.CreateTranslation(
                    new Vector3(2f*x/testGridDensity + offsetX, 0f, -2f*z/testGridDensity + offsetZ));
                float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
                AssetsEngine._sh_Lit.SetMatrix4(Model, mesh_uModel);
                AssetsEngine._sh_Lit.SetFloat(Roughness, 1f - x/testGridDensity/testGridCount);
                AssetsEngine._sh_Lit.SetFloat(Metallic, z/testGridDensity/testGridCount);
                AssetsEngine._mesh_Sphere.Draw();
            }
        }
    }

}
