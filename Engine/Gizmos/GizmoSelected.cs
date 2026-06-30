using System.Numerics;
using Silk.NET.OpenGL;
using Engine.Input;
using Engine.Graphics.UI;
using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public class GizmoSelected : IGizmoWorld {
    public GizmoSelected () {
        GL = Renderer.Instance.GL;
        _sh_Outline = Renderer.Instance._sh_Outline;
    }

    GL GL = null!;
    Shader _sh_Outline = null!;


    public MeshComponent? selectedMesh = null;
    public bool selectedGizmoLocal = false;
    private const string OutlineColor = "uOutlineColor";

    public Vector3 selectedDragPos;
    public Vector3 selectedDragRot;
    public Vector3 selectedDragMargin;

    public SelectedGizmoMode selectedGizmoMode = SelectedGizmoMode.Position;
    public SelectedPositionGizmoMode selectedPositionMode;
    public SelectedRotationGizmoMode selectedRotationMode;
    public SelectedScaleGizmoMode selectedScaleMode;

    /// Drawa Info
    public bool xOver = false;
    public bool yOver = false;
    public bool zOver = false;
    public bool xyOver = false;
    public bool xzOver = false;
    public bool yzOver = false;
    public Vector3 quadXYPos = Vector3.Zero;
    public Vector3 quadXZPos = Vector3.Zero;
    public Vector3 quadYZPos = Vector3.Zero;
    public Vector3 quadXYRot = Vector3.Zero;
    public Vector3 quadXZRot = Vector3.Zero;
    public Vector3 quadYZRot = Vector3.Zero;
    public Vector3 quadScale = Vector3.Zero;


    public void Update () {
        if (selectedMesh is null) return;

        Shader _sh_Unlit = Renderer.Instance._sh_Unlit;
        Renderer.Instance.GL.Disable(EnableCap.DepthTest);
        Renderer.Instance.GL.Disable(EnableCap.CullFace);

        TransformComponent tr_obj = selectedMesh.owner.Transform;
        Vector3 camPos = Camera.Instance.cameraPos;
        Vector3 _objPos = tr_obj.Position;
        Vector3 _objRot = tr_obj.Rotation;
        float _dist = Vector3.Distance(camPos, _objPos);

        _sh_Unlit.Use();
        _sh_Unlit.SetMatrix4(View, Renderer.Instance.UView);
        _sh_Unlit.SetMatrix4(Projection, Renderer.Instance.UProjection);
        _sh_Unlit.SetVector3(ViewPos, camPos);

        Ray ray = Camera.Instance.RaycastMouse();
        float _squareSize = 0.025f;
        float half = 0.5f*_squareSize;
        Vector3 quadXYOffset;
        Vector3 quadXZOffset;
        Vector3 quadYZOffset;
        Vector3 quadXYRot;
        Vector3 quadXZRot;
        Vector3 quadYZRot;
        Vector3? squarePickXYPos;
        Vector3? squarePickXZPos;
        Vector3? squarePickYZPos;
        quadScale =_dist*_squareSize*Vector3.One;
        //if (selectedGizmoLocal) Renderer.Instance._m4x4_quadsScale *= Matrix4x4.RotationEuler(_objRot);
        switch (selectedGizmoMode) {
            case SelectedGizmoMode.Position:
                quadXYOffset = new Vector3(0.5f, 0.5f, 0);
                quadXZOffset = new Vector3(0.5f, 0, 0.5f);
                quadYZOffset = new Vector3(0, 0.5f, 0.5f);
                if (camPos.X < _objPos.X) {
                    quadXYOffset.X *= -1;
                    quadXZOffset.X *= -1;
                }
                if (camPos.Y < _objPos.Y) {
                    quadXYOffset.Y *= -1;
                    quadYZOffset.Y *= -1;
                }
                if (camPos.Z < _objPos.Z) {
                    quadXZOffset.Z *= -1;
                    quadYZOffset.Z *= -1;
                }
                quadXYRot = new(90, 0, 0);
                quadXZRot = Vector3.Zero;
                quadYZRot = new(0, 0, 90);

                /// XY
                squarePickXYPos = TryPickQuad(quadXYOffset, Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY);
                xyOver = squarePickXYPos is not null;
                if (squarePickXYPos is not null) {
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        //Log.log("XY");
                        selectedPositionMode = SelectedPositionGizmoMode.XY;
                        selectedDragMargin = _objPos - squarePickXYPos!.Value;
                        //DragStartValues();
                    }
                }

                /// XZ
                squarePickXZPos = TryPickQuad(quadXZOffset, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ);
                xzOver = squarePickXZPos is not null;
                if (squarePickXZPos is not null) {
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        //Log.log("XZ");
                        selectedPositionMode = SelectedPositionGizmoMode.XZ;
                        selectedDragMargin = _objPos - squarePickXZPos!.Value;
                        //DragStartValues();
                    }
                }

                /// YZ
                squarePickYZPos = TryPickQuad(quadYZOffset, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
                yzOver = squarePickYZPos is not null;
                if (squarePickYZPos is not null) {
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        //Log.log("YZ");
                        selectedPositionMode = SelectedPositionGizmoMode.YZ;
                        selectedDragMargin = _objPos - squarePickYZPos!.Value;
                        //DragStartValues();
                    }
                }

                if (Inputs.Actions[Inputs.LMB].pressed) {
                    Vector3? pos = null;
                    switch (selectedPositionMode) {
                        /*case SelectedPositionGizmoMode.X:
                            break;
                        case SelectedPositionGizmoMode.Y:
                            break;
                        case SelectedPositionGizmoMode.Z:
                            break;*/
                        case SelectedPositionGizmoMode.XY:
                            pos = Raycaster.IntersectPlane(ray, _objPos, tr_obj.Forward);
                            break;
                        case SelectedPositionGizmoMode.XZ:
                            pos = Raycaster.IntersectPlane(ray, _objPos, tr_obj.Up);
                            break;
                        case SelectedPositionGizmoMode.YZ:
                            pos = Raycaster.IntersectPlane(ray, _objPos, tr_obj.Right);
                            break;
                    }
                    if (pos is not null) {
                        selectedMesh.owner.Transform.Position = pos.Value + selectedDragMargin;
                    }
                } else if (Inputs.Actions[Inputs.LMB].pressedUp) {
                    selectedPositionMode = SelectedPositionGizmoMode.None;
                }

                quadXYPos = _objPos + _dist*_squareSize*quadXYOffset;
                quadXZPos = _objPos + _dist*_squareSize*quadXZOffset;
                quadYZPos = _objPos + _dist*_squareSize*quadYZOffset;
                this.quadXYRot = quadXYRot;
                this.quadXZRot = quadXZRot;
                this.quadYZRot = quadYZRot;
                break;
                /*case SelectedGizmoMode.Rotation:
                    break;*/
                /*case SelectedGizmoMode.Scale:
                    break;*/
        }

        TextRenderer.AddText($"Selected P: {tr_obj.Position:F3}");
        TextRenderer.AddText($"Selected R: {tr_obj.Rotation:F3}");
        TextRenderer.AddText($"Selected S: {tr_obj.Scale:F3}");

        Vector3? TryPickQuad (Vector3 offset, Vector3 normal, Vector3 axisA, Vector3 axisB) {
            Vector3 quadCenter = _objPos + _dist*offset*_squareSize;
            float halfExtent = _dist*half;
            Vector3? hit = Raycaster.IntersectPlane(ray, quadCenter, normal);
            if (hit is null) return null;

            Vector3 local = hit.Value - quadCenter;
            float a = Vector3.Dot(local, axisA);
            float b = Vector3.Dot(local, axisB);

            if (MathF.Abs(a) <= halfExtent && MathF.Abs(b) <= halfExtent) return hit;
            else return null;
        }
    }


    public void Draw () {
        DrawSelectedOutline();
        DrawSelectedGizmo();
    }

    public void DrawSelectedGizmo () {
        if (selectedMesh is null) return;

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);

        TransformComponent tr_obj = selectedMesh.owner.Transform;
        Vector3 camPos = Camera.Instance.cameraPos;
        Vector3 _objPos = tr_obj.Position;
        Vector3 _objRot = tr_obj.Rotation;
        float _dist = Vector3.Distance(camPos, _objPos);

        Shader _sh_Unlit = Renderer.Instance._sh_Unlit;
        _sh_Unlit.Use();
        _sh_Unlit.SetMatrix4(View, Renderer.Instance.UView);
        _sh_Unlit.SetMatrix4(Projection, Renderer.Instance.UProjection);
        if (xyOver) drawQuad(quadXYPos, quadXYRot, Constants.blueLight);
        else drawQuad(quadXYPos, quadXYRot, Constants.blue);
        if (xzOver) drawQuad(quadXZPos, quadXZRot, Constants.greenLight);
        else drawQuad(quadXZPos, quadXZRot, Constants.green);
        if (yzOver) drawQuad(quadYZPos, quadYZRot, Constants.redLight);
        else drawQuad(quadYZPos, quadYZRot, Constants.red);
        void drawQuad (Vector3 pos, Vector3 rot, Vector3 color) {
            Matrix4x4 m4x4_selected = Matrix4x4.CreateScale(quadScale)*Matrix4x4.RotationEuler(rot)*Matrix4x4.Position(pos);
            _sh_Unlit.SetMatrix4(Model, Matrix4x4.ToArray(m4x4_selected));
            _sh_Unlit.SetFloat(Alpha, 0.5f);
            _sh_Unlit.SetColor(Color, color);
            Renderer.Instance._mesh_PlaneQuad.Draw();
        }

        /// <> change to 3-lines
        float _axesSize = 0.1f;
        Matrix4x4 m4x4_selected = Matrix4x4.Identity;
        m4x4_selected = m4x4_selected*Matrix4x4.Position(selectedMesh.owner.Transform.Position);
        float[] mesh_uModel = Matrix4x4.ToArray(m4x4_selected);
        Shader _sh_Axes = Renderer.Instance._sh_Axes;
        _sh_Axes.Use();
        _sh_Axes.SetMatrix4(View, Renderer.Instance.UView);
        _sh_Axes.SetMatrix4(Projection, Renderer.Instance.UProjection);
        _sh_Axes.SetVector3(ViewPos, Camera.Instance.cameraPos);
        _sh_Axes.SetMatrix4(Model, mesh_uModel);
        _sh_Axes.SetVector3(CameraPos, selectedMesh.owner.Transform.Position); /// <> rework
        _sh_Axes.SetFloat(Alpha, 0.5f);
        _sh_Axes.SetFloat(Radius, _axesSize*_dist); /// <> rework
        _sh_Axes.SetFloat(Fade, _axesSize*_dist); /// <> rework
        Renderer.Instance._mesh_AxesWireframe.Draw(PrimitiveType.Lines);
    }

    public void DrawSelectedOutline () {
        if (selectedMesh is null) return;

        RenderInfo renderInfo = selectedMesh.CreateRenderInfo;

        GL.Enable(EnableCap.StencilTest);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);

        /// Pass 1 — render mesh normally, mark stencil = 1 everywhere it's visible
        GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        GL.StencilMask(0xFF);
        GL.DepthMask(true);

        Renderer.Instance.DrawMesh(renderInfo);

        /// Pass 2 — outline: draw inflated mesh ONLY where stencil != 1
        GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
        GL.StencilMask(0x00);
        GL.DepthMask(false);

        float width = 2.5f;
        float dist = Vector3.Distance(Camera.Instance.cameraPos, renderInfo.pos);
        float t = width*0.001f*MathF.Sqrt(dist);
        Vector3 outlineScale = new Vector3(
            renderInfo.scale.X + t,
            renderInfo.scale.Y + t,
            renderInfo.scale.Z + t
        );

        Matrix4x4 mesh_m4x4 = Matrix4x4.CreateScale(outlineScale)
            *Matrix4x4.RotationEuler(renderInfo.rot)*Matrix4x4.Position(renderInfo.pos);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);

        _sh_Outline.Use();
        _sh_Outline.SetMatrix4(View, Renderer.Instance.UView);
        _sh_Outline.SetMatrix4(Projection, Renderer.Instance.UProjection);
        _sh_Outline.SetMatrix4(Model, mesh_uModel);
        _sh_Outline.SetVector3(OutlineColor, Constants.cyan);

        renderInfo.mesh!.Draw();

        /// Restore state
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.StencilMask(0xFF);
        GL.Disable(EnableCap.StencilTest);
    }

}
