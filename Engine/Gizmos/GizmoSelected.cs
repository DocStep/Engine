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
    public bool selectedGizmoLocal = true;
    private const string OutlineColor = "uOutlineColor";

    public Vector3 selectedDragPos;
    public Vector3 selectedDragRot;
    public Vector3 selectedDragMargin;

    public SelectedGizmoMode selectedGizmoMode = SelectedGizmoMode.Position;
    public SelectedPositionGizmoMode selectedPositionMode;
    public SelectedRotationGizmoMode selectedRotationMode;
    public SelectedScaleGizmoMode selectedScaleMode;

    /// Draw Info
    public bool xOver = false;
    public bool yOver = false;
    public bool zOver = false;
    public bool xyOver = false;
    public bool xzOver = false;
    public bool yzOver = false;
    public Vector3 quadXYPos = Vector3.Zero;
    public Vector3 quadXZPos = Vector3.Zero;
    public Vector3 quadYZPos = Vector3.Zero;
    public Matrix4x4 quadXYBasis = Matrix4x4.Identity;
    public Matrix4x4 quadXZBasis = Matrix4x4.Identity;
    public Matrix4x4 quadYZBasis = Matrix4x4.Identity;
    public Vector3 quadScale = Vector3.Zero;


    static Matrix4x4 BasisToWorld (Vector3 localX, Vector3 localY, Vector3 localZ) {
        return new Matrix4x4(
            localX.X, localX.Y, localX.Z, 0,
            localY.X, localY.Y, localY.Z, 0,
            localZ.X, localZ.Y, localZ.Z, 0,
            0, 0, 0, 1
        );
    }


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
        Vector3? squarePickXYPos;
        Vector3? squarePickXZPos;
        Vector3? squarePickYZPos;
        quadScale = _dist*_squareSize*Vector3.One;

        switch (selectedGizmoMode) {
            case SelectedGizmoMode.Position:
                Vector3 right = selectedGizmoLocal ? tr_obj.Right : Vector3.UnitX;
                Vector3 up = selectedGizmoLocal ? tr_obj.Up : Vector3.UnitY;
                Vector3 forward = selectedGizmoLocal ? tr_obj.Forward : Vector3.UnitZ;

                Vector3 toCam = camPos - _objPos;
                float signR = Vector3.Dot(toCam, right) < 0 ? -1f : 1f;
                float signU = Vector3.Dot(toCam, up) < 0 ? -1f : 1f;
                float signF = Vector3.Dot(toCam, forward) < 0 ? -1f : 1f;

                quadXYOffset = signR*right*0.5f + signU*up*0.5f;
                quadXZOffset = signR*right*0.5f + signF*forward*0.5f;
                quadYZOffset = signU*up*0.5f + signF*forward*0.5f;

                /// XY plane -> normal = forward, in-plane axes = right/up
                quadXYBasis = BasisToWorld(right, forward, up);
                /// XZ plane -> normal = up, in-plane axes = right/forward
                quadXZBasis = BasisToWorld(right, up, forward);
                /// YZ plane -> normal = right, in-plane axes = up/forward
                quadYZBasis = BasisToWorld(up, right, forward);

                //Vector3 debugPos = Vector3.One;
                //Debug.Line(debugPos, debugPos + right, Constants.red);
                //Debug.Line(debugPos, debugPos + up, Constants.green);
                //Debug.Line(debugPos, debugPos + forward, Constants.blue);

                /// XY
                squarePickXYPos = TryPickQuad(quadXYOffset, forward, right, up);
                xyOver = squarePickXYPos is not null;
                if (squarePickXYPos is not null) {
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.XY;
                        selectedDragPos = _objPos;
                        selectedDragRot = _objRot;
                        selectedDragMargin = _objPos - squarePickXYPos.Value;
                    }
                }
                /// XZ
                squarePickXZPos = TryPickQuad(quadXZOffset, up, right, forward);
                xzOver = squarePickXZPos is not null;
                if (squarePickXZPos is not null) {
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.XZ;
                        selectedDragPos = _objPos;
                        selectedDragRot = _objRot;
                        selectedDragMargin = _objPos - squarePickXZPos!.Value;
                    }
                }
                /// YZ
                squarePickYZPos = TryPickQuad(quadYZOffset, right, up, forward);
                yzOver = squarePickYZPos is not null;
                if (squarePickYZPos is not null) {
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.YZ;
                        selectedDragPos = _objPos;
                        selectedDragRot = _objRot;
                        selectedDragMargin = _objPos - squarePickYZPos!.Value;
                    }
                }

                if (Inputs.Actions[Inputs.LMB].pressed) {
                    if (selectedPositionMode != SelectedPositionGizmoMode.None) {
                        Vector3? pos = null;
                        switch (selectedPositionMode) {
                            case SelectedPositionGizmoMode.XY:
                                pos = Raycast.IntersectPlane(ray, selectedDragPos, forward);
                                break;
                            case SelectedPositionGizmoMode.XZ:
                                pos = Raycast.IntersectPlane(ray, selectedDragPos, up);
                                break;
                            case SelectedPositionGizmoMode.YZ:
                                pos = Raycast.IntersectPlane(ray, selectedDragPos, right);
                                break;
                        }
                        if (pos is not null) {
                            selectedMesh.owner.Transform.SetPosition(pos.Value + selectedDragMargin);
                            //Log.log(pos.Value + selectedDragMargin);
                            //Debug.Line(debugPos, debugPos - selectedDragMargin, Constants.black);
                            //Debug.Line(pos.Value, pos.Value + selectedDragMargin, Constants.black);
                            //TextRenderer.AddText($"_margin: {selectedDragMargin:F3}");
                        }
                    }
                } else if (Inputs.Actions[Inputs.LMB].pressedUp) {
                    selectedPositionMode = SelectedPositionGizmoMode.None;
                }

                quadXYPos = _objPos + _dist*_squareSize*quadXYOffset;
                quadXZPos = _objPos + _dist*_squareSize*quadXZOffset;
                quadYZPos = _objPos + _dist*_squareSize*quadYZOffset;
                break;
                /*case SelectedGizmoMode.Rotation:
                    break;*/
                /*case SelectedGizmoMode.Scale:
                    break;*/
        }

        TextRenderer.AddText($"Selected:");
        TextRenderer.AddText($"Position: {tr_obj.Position:F3}");
        TextRenderer.AddText($"Rotation: {tr_obj.Rotation:F3}");
        TextRenderer.AddText($"Scale: {tr_obj.Scale:F3}");

        Vector3? TryPickQuad (Vector3 offset, Vector3 normal, Vector3 axisA, Vector3 axisB) {
            Vector3 quadCenter = _objPos + _dist*offset*_squareSize;
            float halfExtent = _dist*half;
            Vector3? hit = Raycast.IntersectPlane(ray, quadCenter, normal);
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

        /// Quads
        drawQuad(quadXYPos, quadXYBasis, xyOver ? Constants.blueLight : Constants.blue);
        drawQuad(quadXZPos, quadXZBasis, xzOver ? Constants.greenLight : Constants.green);
        drawQuad(quadYZPos, quadYZBasis, yzOver ? Constants.redLight : Constants.red);

        void drawQuad (Vector3 pos, Matrix4x4 basis, Vector3 color) {
            Matrix4x4 m4x4_selected = Matrix4x4.CreateScale(quadScale)*basis*Matrix4x4.Position(pos);
            _sh_Unlit.SetMatrix4(Model, Matrix4x4.ToArray(m4x4_selected));
            _sh_Unlit.SetFloat(Alpha, 0.5f);
            _sh_Unlit.SetColor(Color, color);
            Renderer.Instance._mesh_PlaneQuad.Draw();
        }

        /// Axes
        float _axesSize = 0.15f;
        Vector3 pos2 = selectedMesh.owner.Transform.Position;
        Matrix4x4 _m4x4_selectedScale = Matrix4x4.CreateScale(_dist*_axesSize);

        Draw(Vector3.Zero, Constants.blue);
        Draw(new Vector3(-90, 0, 0), Constants.green);
        Draw(new Vector3(0, 90, 0), Constants.red);

        void Draw (Vector3 rot, Vector3 color) {
            Matrix4x4 m4x4_selected = _m4x4_selectedScale
                *Matrix4x4.RotationEuler(rot)*Matrix4x4.RotationEuler(_objRot)*Matrix4x4.Position(pos2);
            float[] mesh_uModel = Matrix4x4.ToArray(m4x4_selected);
            _sh_Unlit.SetMatrix4(Model, mesh_uModel);
            _sh_Unlit.SetFloat(Alpha, 0.5f);
            _sh_Unlit.SetColor(Color, color);
            Renderer.Instance._mesh_Arrow3D.Draw();
        }
    }

    public void DrawSelectedOutline () {
        if (selectedMesh is null) return;

        RenderInfo renderInfo = selectedMesh.CreateRenderInfo;
        if (renderInfo.mesh is null) return;

        GL.Enable(EnableCap.StencilTest);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);

        /// Pass 1 — Render mesh normally, mark stencil = 1 everywhere it's visible
        GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        GL.StencilMask(0xFF);
        GL.DepthMask(true);

        Renderer.Instance.DrawMesh(renderInfo);

        /// Pass 2 — Outline: draw inflated mesh ONLY where stencil != 1
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

        Matrix4x4 m4x4_mesh = Matrix4x4.CreateScale(outlineScale)
            *Matrix4x4.RotationEuler(renderInfo.rot)*Matrix4x4.Position(renderInfo.pos);
        float[] mesh_uModel = Matrix4x4.ToArray(m4x4_mesh);

        _sh_Outline.Use();
        _sh_Outline.SetMatrix4(View, Renderer.Instance.UView);
        _sh_Outline.SetMatrix4(Projection, Renderer.Instance.UProjection);
        _sh_Outline.SetMatrix4(Model, mesh_uModel);
        _sh_Outline.SetVector3(OutlineColor, Constants.cyan);

        renderInfo.mesh.Draw();

        /// Restore State
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.StencilMask(0xFF);
        GL.Disable(EnableCap.StencilTest);
    }

}