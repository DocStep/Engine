using Silk.NET.OpenGL;
using Engine.Input;
using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public class GizmoSelected : IGizmoWorld {
    public GizmoSelected () {
        GL = Renderer.GL;
        _sh_Outline = Gizmos._sh_Outline;
    }

    GL GL = null!;
    Shader _sh_Outline = null!;

    public MeshComponent? selectedMesh = null;
    private const string OutlineColor = "uOutlineColor";

    public SelectedGizmoMode selectedGizmoMode = SelectedGizmoMode.Position;
    public SelectedPositionGizmoMode selectedPositionMode;
    public SelectedRotationGizmoMode selectedRotationMode;
    public SelectedScaleGizmoMode selectedScaleMode;

    private const float _squareSize = 0.025f;
    private const float _axisLength = 0.15f;
    private const float _axisRadius = 0.005f;
    private const float _width = 10f;

    public Vector3 selectedDragPos;
    public Vector3 selectedDragRot;
    public Vector3 selectedDragMargin;
    public bool selectedGizmoWorldSpace = true;
    private bool isMouseBlocked = false;

    /// Draw Gizmos
    public SelectedPositionGizmoMode selectedPositionOverMode;
    private Vector3 quadXYPos;
    private Vector3 quadXZPos;
    private Vector3 quadYZPos;
    private Matrix4x4 quadXYBasis;
    private Matrix4x4 quadXZBasis;
    private Matrix4x4 quadYZBasis;
    private Vector3 quadScale;

    private Vector3 gizmoRight;
    private Vector3 gizmoUp;
    private Vector3 gizmoForward;

    /// Draw
    //private Vector3 drawPos;
    //private Vector3 drawRot;
    //private Vector3 drawScale;


    public void Update () {
        if (selectedMesh is null) return;

        Ray? rayOpt = Camera.Instance.RaycastMouse();
        if (rayOpt is null) {
            /// Mouse left the Scene panel mid-drag — release cleanly rather than leaving picking state stuck
            if (selectedPositionMode != SelectedPositionGizmoMode.None && !Inputs.Actions[Inputs.LMB].pressed) {
                selectedPositionMode = SelectedPositionGizmoMode.None;
                if (isMouseBlocked) {
                    isMouseBlocked = false;
                    CameraEditor.Instance?.UnblockMouse(this);
                }
            }
            return;
        }
        Ray ray = rayOpt.Value;

        Transform tr_obj = selectedMesh.owner.Transform;
        Vector3 camPos = Camera.Instance.cameraPos;
        Vector3 _objPos = tr_obj.Position;
        Vector3 _objRot = tr_obj.Rotation;
        float _dist = Vector3.Distance(camPos, _objPos);

        float half = 0.5f*_squareSize;
        Vector3 axisCapsuleXOffset;
        Vector3 axisCapsuleYOffset;
        Vector3 axisCapsuleZOffset;
        Vector3 quadXYOffset;
        Vector3 quadXZOffset;
        Vector3 quadYZOffset;
        quadScale = _dist*_squareSize*Vector3.One;

        if (Inputs.Actions[Inputs.GizmoLocal].pressedDown) {
            selectedGizmoWorldSpace = !selectedGizmoWorldSpace;
        }

        switch (selectedGizmoMode) {
            case SelectedGizmoMode.Position:
                if (selectedPositionMode == SelectedPositionGizmoMode.None) {
                    gizmoRight = selectedGizmoWorldSpace ? Vector3.UnitX : tr_obj.Right;
                    gizmoUp = selectedGizmoWorldSpace ? Vector3.UnitY : tr_obj.Up;
                    gizmoForward = selectedGizmoWorldSpace ? Vector3.UnitZ : tr_obj.Forward;
                }
                selectedPositionOverMode = SelectedPositionGizmoMode.None;

                axisCapsuleXOffset = tr_obj.Position + gizmoRight*0.5f*_dist*_axisLength;
                axisCapsuleYOffset = tr_obj.Position + gizmoUp*0.5f*_dist*_axisLength;
                axisCapsuleZOffset = tr_obj.Position + gizmoForward*0.5f*_dist*_axisLength;

                /// X
                Vector3? axisPickXPos = TryPickCapsule(gizmoRight, _axisLength, _axisRadius);
                if (axisPickXPos is not null) {
                    selectedPositionOverMode = SelectedPositionGizmoMode.X;
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.X;
                        selectedDragMargin = _objPos - axisPickXPos.Value;
                    }
                }
                /// Y
                Vector3? axisPickYPos = TryPickCapsule(gizmoUp, _axisLength, _axisRadius);
                if (axisPickYPos is not null) {
                    selectedPositionOverMode = SelectedPositionGizmoMode.Y;
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.Y;
                        selectedDragMargin = _objPos - axisPickYPos.Value;
                    }
                }
                /// Z
                Vector3? axisPickZPos = TryPickCapsule(gizmoForward, _axisLength, _axisRadius);
                if (axisPickZPos is not null) {
                    selectedPositionOverMode = SelectedPositionGizmoMode.Z;
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.Z;
                        selectedDragMargin = _objPos - axisPickZPos.Value;
                    }
                }

                /// Planes
                Vector3 toCam = camPos - _objPos;
                float signRight = Vector3.Dot(toCam, gizmoRight) < 0 ? -1 : 1;
                float signUp = Vector3.Dot(toCam, gizmoUp) < 0 ? -1 : 1;
                float signFront = Vector3.Dot(toCam, gizmoForward) < 0 ? -1 : 1;

                quadXYOffset = signRight*gizmoRight*0.5f + signUp*gizmoUp*0.5f;
                quadXZOffset = signRight*gizmoRight*0.5f + signFront*gizmoForward*0.5f;
                quadYZOffset = signUp*gizmoUp*0.5f + signFront*gizmoForward*0.5f;

                /// Plane XY, Normal = forward
                quadXYBasis = BasisToWorld(gizmoRight, gizmoForward, gizmoUp);
                /// Plane XZ, Normal = up
                quadXZBasis = BasisToWorld(gizmoRight, gizmoUp, gizmoForward);
                /// Plane YZ, Normal = right
                quadYZBasis = BasisToWorld(gizmoUp, gizmoRight, gizmoForward);

                /// XY
                Vector3? squarePickXYPos = TryPickQuad(quadXYOffset, gizmoForward, gizmoRight, gizmoUp);
                if (squarePickXYPos is not null) {
                    selectedPositionOverMode = SelectedPositionGizmoMode.XY;
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.XY;
                        selectedDragMargin = _objPos - squarePickXYPos.Value;
                    }
                }
                /// XZ
                Vector3? squarePickXZPos = TryPickQuad(quadXZOffset, gizmoUp, gizmoRight, gizmoForward);
                if (squarePickXZPos is not null) {
                    selectedPositionOverMode = SelectedPositionGizmoMode.XZ;
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.XZ;
                        selectedDragMargin = _objPos - squarePickXZPos!.Value;
                    }
                }
                /// YZ
                Vector3? squarePickYZPos = TryPickQuad(quadYZOffset, gizmoRight, gizmoUp, gizmoForward);
                if (squarePickYZPos is not null) {
                    selectedPositionOverMode = SelectedPositionGizmoMode.YZ;
                    if (Inputs.Actions[Inputs.LMB].pressedDown) {
                        selectedPositionMode = SelectedPositionGizmoMode.YZ;
                        selectedDragMargin = _objPos - squarePickYZPos!.Value;
                    }
                }

                if (Inputs.Actions[Inputs.LMB].pressedDown) {
                    if (selectedPositionMode != SelectedPositionGizmoMode.None)
                        isMouseBlocked |= true;
                    selectedDragPos = _objPos;
                    selectedDragRot = _objRot;
                }
                if (Inputs.Actions[Inputs.LMB].pressed) {
                    /// Hold
                    if (selectedPositionMode != SelectedPositionGizmoMode.None) {
                        Vector3? pos = null;
                        switch (selectedPositionMode) {
                            case SelectedPositionGizmoMode.X:
                                pos = Raycast.ClosestPointRayToAxis(ray, selectedDragPos, gizmoRight);
                                break;
                            case SelectedPositionGizmoMode.Y:
                                pos = Raycast.ClosestPointRayToAxis(ray, selectedDragPos, gizmoUp);
                                break;
                            case SelectedPositionGizmoMode.Z:
                                pos = Raycast.ClosestPointRayToAxis(ray, selectedDragPos, gizmoForward);
                                break;
                            case SelectedPositionGizmoMode.XY:
                                pos = Raycast.IntersectPlane(ray, selectedDragPos, gizmoForward);
                                break;
                            case SelectedPositionGizmoMode.XZ:
                                pos = Raycast.IntersectPlane(ray, selectedDragPos, gizmoUp);
                                break;
                            case SelectedPositionGizmoMode.YZ:
                                pos = Raycast.IntersectPlane(ray, selectedDragPos, gizmoRight);
                                break;
                        }
                        if (pos is not null) {
                            selectedMesh.owner.Transform.Stop();
                            selectedMesh.owner.Transform.SetPosition(pos.Value + selectedDragMargin);
                            selectedMesh.owner.Transform.SetRotation(selectedDragRot);
                        }
                    }
                } else if (Inputs.Actions[Inputs.LMB].pressedUp) {
                    /// Release
                    selectedPositionMode = SelectedPositionGizmoMode.None;
                    //selectedMesh.owner.Transform.SetRotation(selectedDragRot);
                    if (isMouseBlocked) {
                        isMouseBlocked = false;
                        CameraEditor.Instance?.UnblockMouse(this);
                    }
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

        if (isMouseBlocked) {
            CameraEditor.Instance?.BlockMouse(this);
        } else {
            CameraEditor.Instance?.UnblockMouse(this);
        }

        Vector3? TryPickCapsule (Vector3 axisDir, float length, float radius) {
            Vector3 segStart = _objPos;
            Vector3 segDir = Vector3.Normalize(axisDir);
            float segLen = _dist*length;
            float capRadius = _dist*radius;

            Vector3 rDir = Vector3.Normalize(ray.Direction);
            Vector3 w0 = ray.Origin - segStart;

            float b = Vector3.Dot(rDir, segDir);
            float d = Vector3.Dot(rDir, w0);
            float e = Vector3.Dot(segDir, w0);
            float denom = 1f - b*b;

            float tRay, tSeg;
            if (MathF.Abs(denom) < 1e-6f) {
                tRay = 0f;
                tSeg = e;
            } else {
                tRay = (b*e - d)/denom;
                tSeg = (e - b*d)/denom;
            }

            tSeg = Math.Clamp(tSeg, 0f, segLen);
            tRay = MathF.Max(tRay, 0f);

            Vector3 pointOnRay = ray.Origin + tRay*rDir;
            Vector3 pointOnSeg = segStart + tSeg*segDir;

            if (Vector3.Distance(pointOnRay, pointOnSeg) <= capRadius) return pointOnSeg;
            return null;
        }
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


    public static Matrix4x4 BasisToWorld (Vector3 localX, Vector3 localY, Vector3 localZ) {
        return new Matrix4x4(
            localX.X, localX.Y, localX.Z, 0,
            localY.X, localY.Y, localY.Z, 0,
            localZ.X, localZ.Y, localZ.Z, 0,
            0, 0, 0, 1
        );
    }


    public void Draw () {
        Transform? tr_obj = selectedMesh?.owner.Transform;
        if (tr_obj is null) return;

        GL.Viewport(0, 0, (uint)Renderer.Instance.PostProcessStack.Width, (uint)Renderer.Instance.PostProcessStack.Height);

        Renderer.GL.Disable(EnableCap.DepthTest);
        Renderer.GL.Disable(EnableCap.CullFace);

        Shader _sh_Unlit = AssetsEngine._sh_Unlit;
        _sh_Unlit.Use();
        _sh_Unlit.SetMatrix4(View, Renderer.Instance.UView);
        _sh_Unlit.SetMatrix4(Projection, Renderer.Instance.UProjection);
        _sh_Unlit.SetVector3(ViewPos, Camera.Instance.cameraPos);

        DrawOutline();
        DrawGizmo();
    }

    private void DrawGizmo () {
        if (selectedMesh is null) return;

        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        //GL.DepthRange(0, 0.9999f);

        Transform tr_obj = selectedMesh.owner.Transform;
        Vector3 camPos = Camera.Instance.cameraPos;
        Vector3 _objPos = tr_obj.Position;
        Vector3 _objRot = tr_obj.Rotation;
        float _dist = Vector3.Distance(camPos, _objPos);
        bool isColorSelected;

        Shader _sh_Unlit = AssetsEngine._sh_Unlit;
        _sh_Unlit.Use();
        _sh_Unlit.SetMatrix4(View, Renderer.Instance.UView);
        _sh_Unlit.SetMatrix4(Projection, Renderer.Instance.UProjection);

        /// Quads
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.XY || selectedPositionOverMode == SelectedPositionGizmoMode.XY;
        drawQuad(quadXYPos, quadXYBasis, selectedPositionOverMode == SelectedPositionGizmoMode.XY ? Constants.blueLight : Constants.blue);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.XZ || selectedPositionOverMode == SelectedPositionGizmoMode.XZ;
        drawQuad(quadXZPos, quadXZBasis, selectedPositionOverMode == SelectedPositionGizmoMode.XZ ? Constants.greenLight : Constants.green);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.YZ || selectedPositionOverMode == SelectedPositionGizmoMode.YZ;
        drawQuad(quadYZPos, quadYZBasis, selectedPositionOverMode == SelectedPositionGizmoMode.YZ ? Constants.redLight : Constants.red);

        void drawQuad (Vector3 pos, Matrix4x4 basis, Vector3 color) {
            Matrix4x4 m4x4_selected = Matrix4x4.CreateScale(quadScale)*basis*Matrix4x4.Position(pos);
            _sh_Unlit.SetMatrix4(Model, Matrix4x4.ToArray(m4x4_selected));
            _sh_Unlit.SetVector3(Color, color); 
            _sh_Unlit.SetFloat(Alpha, 0.5f);
            AssetsEngine._mesh_PlaneQuad.Draw();
        }


        /// Axes
        Matrix4x4 gizmoBasis = BasisToWorld(gizmoRight, gizmoUp, gizmoForward);
        Vector3 pos3 = selectedMesh.owner.Transform.Position;
        Matrix4x4 _m4x4_selectedScale = Matrix4x4.CreateScale(_dist*_axisLength);

        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.X || selectedPositionOverMode == SelectedPositionGizmoMode.X;
        drawArrow(new Vector3(0, 90, 0), isColorSelected ? Constants.redLight : Constants.red);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.Y || selectedPositionOverMode == SelectedPositionGizmoMode.Y;
        drawArrow(new Vector3(-90, 0, 0), isColorSelected ? Constants.greenLight : Constants.green);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.Z || selectedPositionOverMode == SelectedPositionGizmoMode.Z;
        drawArrow(Vector3.Zero, isColorSelected ? Constants.blueLight : Constants.blue);

        void drawArrow (Vector3 rot, Vector3 color) {
            Matrix4x4 m4x4_selected = _m4x4_selectedScale*Matrix4x4.RotationEuler(rot)*gizmoBasis*Matrix4x4.Position(pos3);
            float[] mesh_uModel = Matrix4x4.ToArray(m4x4_selected);
            _sh_Unlit.SetMatrix4(Model, mesh_uModel);
            _sh_Unlit.SetVector3(Color, color);
            _sh_Unlit.SetFloat(Alpha, 0.5f);
            Gizmos._mesh_Arrow3D.Draw();
        }

        //GL.DepthRange(0, 1);
    }

    private void DrawOutline () {
        if (selectedMesh is null) return;

        RenderInfo renderInfo = selectedMesh.CreateRenderInfo;
        if (renderInfo.mesh is null) return;

        try {
            GL.Enable(EnableCap.StencilTest);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.StencilMask(0xFF);
            GL.Clear(ClearBufferMask.StencilBufferBit);

            /// Pass 1 — mark stencil = 1 where mesh is visible
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.StencilMask(0xFF);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(false);
            //GL.DepthMask(true);
            GL.ColorMask(false, false, false, false);

            Renderer.Instance.DrawInfo(renderInfo);

            /// Pass 2 — draw inflated mesh only where stencil != 1
            GL.ColorMask(true, true, true, true);
            GL.CullFace(TriangleFace.Front);
            GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
            GL.StencilMask(0x00);
            //GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(false);
            //GL.DepthMask(true);

            float dist = Vector3.Distance(Camera.Instance.cameraPos, renderInfo.pos);
            float t = _width*0.001f*MathF.Sqrt(dist);
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
        } finally {
            GL.ColorMask(true, true, true, true);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.DepthMask(true);
            GL.StencilMask(0xFF);
            GL.CullFace(TriangleFace.Back);
            GL.Disable(EnableCap.StencilTest);
        }
    }

}
