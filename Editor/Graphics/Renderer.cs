using Silk.NET.OpenGL;
using Engine.Graphics;
using Engine.Graphics.UI;
using static Engine.Graphics.Shader;
using Shader = Engine.Graphics.Shader;
using static Engine.AssetsEngine;

namespace Editor.Graphics;

/// Camera Matrix
/// Skybox
/// Opaque
/// Transparent
/// PP
/// Gizmos
/// UI

public class Renderer : Engine.Graphics.Renderer {
    public Renderer() : base() {
        Engine.Engine.Instance.de_Update_Engine += EngineUpdate;

        de_LateUpdate += DrawMaterialsGrid;
        de_DrawPostScene += DrawGizmos;
        de_DrawOverlay += DrawGizmosRaw;
    }


    public void EngineUpdate () {
        List<IComponentUpdate> list = ComponentManager.Instance.ComponentsUpdate;
        int count = list.Count;
        for (int c = 0; c < count; c++) {
            if (!list[c].Enabled) continue;
            list[c].Update();
        }
    }
    protected override void DrawSceneAll () {
        switch (Constants.drawMode) {
            case DrawMode.Normal:
                DrawScene();
                break;
            case DrawMode.Wireframe:
                DrawSceneWireframe();
                break;
            case DrawMode.NormalWireframe:
                DrawScene();
                DrawSceneWireframe();
                break;
        }
    }
    protected void DrawSceneWireframe () {
        GL.PolygonMode(TriangleFace.FrontAndBack, GLEnum.Line);

        /// Gizmo
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        /// Both
        GL.Disable(EnableCap.CullFace);

        /// DepthTest
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);

        RenderList.Sort((a, b) => a.material.pass.CompareTo(b.material.pass));
        int count = RenderList.Count;
        for (int i = 0; i < count; i++) {
            RenderInfo info = RenderList[i];
            if (info.material.pass == RenderPass.Opaque || info.material.pass == RenderPass.Transparent)
                DrawInfoWireframe(info);
            else DrawInfo(info);
        }

        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.FrontAndBack, GLEnum.Fill);
    }
    protected void DrawGizmos () {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Disable(EnableCap.CullFace);

        //GL.Disable(EnableCap.DepthTest);

        int count = RenderGizmoList.Count;
        for (int i = 0; i < count; i++) {
            RenderInfo info = RenderGizmoList[i];
            DrawInfo(info);
            Shader shader = info.material.shader;
            //SetSceneUniformsUnlit(shader);
            //SetSceneUniformsLit(shader);
            //SetSceneUniformsSkybox(shader, _skybox.texture, _skybox.maxLod);

            //Matrix4x4 mesh_m4x4 = info.modelOverride ?? Matrix4x4.CreateScale(info.scale)
            //    *Matrix4x4.RotationEuler(info.rot)*Matrix4x4.Position(info.pos);
            //float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
            //shader.SetMatrix4(Model, mesh_uModel);
            //info.material.Apply();

            //info.de_Pre?.Invoke();
            //info.mesh.Draw(info.primitiveType);
            //info.de_Post?.Invoke();
        }

        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.FrontAndBack, GLEnum.Fill);
    }


    protected void DrawInfoWireframe (RenderInfo info) {
        if (info.mesh is null) return;

        Engine.Graphics.Shader shader = Gizmos._mat_GizmoWireframe.shader;

        SetSceneUniformsUnlit(shader);

        Matrix4x4 mesh_m4x4 = info.modelOverride ?? Matrix4x4.CreateScale(info.scale)
            *Matrix4x4.RotationEuler(info.rot)*Matrix4x4.Position(info.pos);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
        shader.SetMatrix4(Model, mesh_uModel);
        Gizmos._mat_GizmoWireframe.Apply();

        info.mesh.Draw(info.primitiveType);

        if (info.depthRangeNear != 0 || info.depthRangeFar != 1)
            GL.DepthRange(0, 1);
    }

    public void DrawGizmosRaw () {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        Gizmos.Draw();
        Gizmos._gizmo_Selected.Draw();
    }



    public static void DrawMaterialsGrid (float offsetX, float offsetZ, int testGridCount = 10, float testGridDensity = 1f) {
        if (!Constants.drawMaterialsGrid) return;

        int total = testGridCount*(int)testGridDensity;
        float speed = 2f;
        for (int x = 0; x < total; x++) {
            for (int z = 0; z < total; z++) {
                float smoothness = (float)x/(total - 1);
                float metallic = (float)z/(total - 1);

                Material mat = new Material(_mat_MaterialPreview);
                mat.SetVector3(Color, Constants.lightGray);
                mat.SetFloat(Smoothness, smoothness);
                mat.SetFloat(Metallic, metallic);

                float _x = x/testGridDensity + offsetX;
                float _z = z/testGridDensity + offsetZ;
                float y = 0.25f*MathF.Sin(_x + speed*(float)Engine.Engine.time) * MathF.Cos(_z + speed*(float)Engine.Engine.time);
                RenderInfo info = new RenderInfo() {
                    pos = new Vector3(_x, y, _z),
                    mesh = _mesh_Sphere,
                    material = mat,
                };
                Renderer.Instance.AddRenderInfo(info);
            }
        }
    }
    public void DrawMaterialsGrid () => DrawMaterialsGrid(-14f, 0f);

}
