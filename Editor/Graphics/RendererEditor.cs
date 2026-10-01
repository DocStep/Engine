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

public class RendererEditor : Renderer {
    public RendererEditor () : base() {
        //Engine.Engine.Instance.de_Update_Engine += EngineUpdate;

        Engine.Engine.Instance.de_AfterUpdate += DrawMaterialsGrid;
        Engine.Engine.Instance.de_AfterUpdate += DrawMaterialGrid;
        //de_DrawPostScene += DrawGizmos;
        de_DrawAfterPostProcess += Gizmos.Draw;
    }


    /*public void EngineUpdate () {
        List<IComponentUpdate> list = ComponentManager.Instance.ComponentsUpdate;
        int count = list.Count;
        for (int c = 0; c < count; c++) {
            if (!list[c].Enabled) continue;
            list[c].Update();
        }
    }*/

    public override void SetTargetSize () {
        Stats.SceneSize = EditorUI.Instance.SceneAvail;
    }
    public override void PresentToBackbuffer () { }

    protected override Camera? MainCamera => CameraEditor.Instance;


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

        /// The queue's raw list is untouched by DrawScene (it only sorts indices), so this
        /// works after it in NormalWireframe mode. Two passes replace the old in-place sort:
        /// scene items as wireframe first, then everything else (UI) the normal way.
        IReadOnlyList<RenderInfo> items = queue.Items;
        int count = items.Count;

        for (int i = 0; i < count; i++) {
            RenderInfo info = items[i];
            if (info.material is null) continue;
            if (info.material.Pass == RenderPass.Opaque || info.material.Pass == RenderPass.Transparent)
                DrawInfoWireframe(info);
        }

        state.Reset(); /// DrawInfoWireframe bound its own shader, so the cache is stale

        for (int i = 0; i < count; i++) {
            RenderInfo info = items[i];
            if (info.material is null) continue;
            if (info.material.Pass != RenderPass.Opaque && info.material.Pass != RenderPass.Transparent)
                DrawRenderInfo(info);
        }

        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.FrontAndBack, GLEnum.Fill);
    }
    /*protected void DrawGizmos () {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Disable(EnableCap.CullFace);

        //GL.Disable(EnableCap.DepthTest);

        int count = RenderGizmoList.Count;
        for (int i = 0; i < count; i++) {
            RenderInfo info = RenderGizmoList[i];
            DrawRenderInfo(info);
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
    }*/


    protected void DrawInfoWireframe (RenderInfo info) {
        if (Renderer.Instance.Camera is null) return;
        if (info.mesh is null) return;

        Shader shader = Gizmos._mat_GizmoWireframe.shader;

        shader.Use();
        SetSceneUniformsUnlit(shader, Renderer.Instance.Camera.CameraPos);

        shader.SetMatrix4x4(uModel, info.model);
        Gizmos._mat_GizmoWireframe.Apply();

        info.mesh.Draw(info.primitiveType);
    }



    /// Built once and reused — a new Material per cell per frame means no batching,
    /// per-frame garbage, and a fast-growing Material.Id
    static Material[] _gridMaterialsA = [];
    static int _gridTotalA = -1;

    static void BuildGridMaterials (ref Material[] cache, ref int cachedTotal, int total) {
        cache = new Material[total*total];
        for (int x = 0; x < total; x++) {
            for (int z = 0; z < total; z++) {
                float smoothness = (float)x/(total - 1);
                float metallic = (float)z/(total - 1);
                Material mat = new Material(_mat_MaterialPreview);
                mat.SetVector3(uColor, Constants.lightGray);
                mat.SetFloat(uSmoothness, smoothness);
                mat.SetFloat(uMetallic, metallic);
                cache[x*total + z] = mat;
            }
        }
        cachedTotal = total;
    }


    private static float[] _gridSinX = null!;
    private static float[] _gridCosZ = null!;
    public static void DrawMaterialsGrid (float offsetX, float offsetZ, int testGridCount = Constants.materialsGridCount, float testGridDensity = 1f) {
        if (!Constants.drawMaterialsGrid) return;

        int total = testGridCount*(int)testGridDensity;
        if (total != _gridTotalA) BuildGridMaterials(ref _gridMaterialsA, ref _gridTotalA, total);

        /// Reused buffers, only reallocated when the grid grows
        if (_gridSinX == null || _gridSinX.Length < total) {
            _gridSinX = new float[total];
            _gridCosZ = new float[total];
        }

        float step = 1f/testGridDensity;
        float t = 2f*(float)Time.time;

        /// Trig depends on one axis only, so compute it once per row/column
        for (int i = 0; i < total; i++) {
            _gridSinX[i] = 0.25f*MathF.Sin(i*step + offsetX + t);
            _gridCosZ[i] = MathF.Cos(i*step + offsetZ + t);
        }

        Renderer renderer = Renderer.Instance;
        Matrix4x4 model = Matrix4x4.Identity;
        Mesh mesh = _mesh_Sphere;
        Material[] materials = _gridMaterialsA;

        for (int x = 0; x < total; x++) {
            float px = x*step + offsetX;
            float sx = _gridSinX[x];
            int row = x*total;
            for (int z = 0; z < total; z++) {
                /// Write translation directly instead of building a new matrix
                model.M41 = px;
                model.M42 = sx*_gridCosZ[z];
                model.M43 = z*step + offsetZ;

                renderer.AddRenderInfo(new RenderInfo() {
                    model = model,
                    mesh = mesh,
                    material = materials[row + z],
                });
            }
        }
    }
    public void DrawMaterialsGrid () => DrawMaterialsGrid(-14f, 0f);

    private static float[] _matGridSinX = null!;
    private static float[] _matGridCosZ = null!;
    public static void DrawMaterialGrid (float offsetX, float offsetZ, int testGridCount = Constants.materialGridCount, float testGridDensity = 1f) {
        if (!Constants.drawMaterialGrid || Renderer.Instance is null || Renderer.Instance.Camera is null) return;
        //return;
        long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
        int total = testGridCount*(int)testGridDensity;

        /// Own buffers, only reallocated when the grid grows
        if (_matGridSinX is null || _matGridSinX.Length < total) {
            _matGridSinX = new float[total];
            _matGridCosZ = new float[total];
        }

        float step = 1f/testGridDensity;
        float t = 2f*(float)Time.time;

        /// Sin depends on x only, Cos on z only: N calls each instead of N*N
        for (int i = 0; i < total; i++) {
            _matGridSinX[i] = 0.25f*MathF.Sin(i*step + offsetX + t);
            _matGridCosZ[i] = MathF.Cos(i*step + offsetZ + t);
        }

        Renderer renderer = Renderer.Instance;
        //Mesh mesh = _mesh_Sphere;
        Mesh mesh = _mesh_PlaneQuad;
        Material material = AssetsEngine._mat_Lit;
        Matrix4x4 model = Matrix4x4.Identity;

        for (int x = 0; x < total; x++) {
            float px = x*step + offsetX;
            float sx = _matGridSinX[x];
            for (int z = 0; z < total; z++) {
                /// Write translation directly instead of CreateTranslation
                model.M41 = px;
                model.M42 = sx*_matGridCosZ[z];
                model.M43 = z*step + offsetZ;

                //mesh = LOD.GetLOD(_mesh_Sphere, Vector3.DistanceSquared(new Vector3(model.M41, model.M42, model.M43), renderer.Camera.CameraPos)),
                renderer.AddRenderInfo(new RenderInfo() {
                    model = model,
                    mesh = mesh,
                    material = material,
                });
            }
        }
        long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
        //Log.log(System.Diagnostics.Stopwatch.GetElapsedTime(t1, t2).TotalMilliseconds);
    }
    public void DrawMaterialGrid () => DrawMaterialGrid(0f, 20f);

}