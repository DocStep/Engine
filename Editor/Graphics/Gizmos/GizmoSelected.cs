using Silk.NET.OpenGL;
using Engine.Input;
using Engine.Graphics;
using static Engine.Graphics.Shader;
using Shader = Engine.Graphics.Shader;

namespace Editor.Graphics;


public class GizmoSelected : IDisposable {
    public GizmoSelected () {
        GL = Renderer.GL;
        RectDebugOutline = new UI.RectGizmo();
        _sh_Outline = Gizmos._sh_Outline;
    }

    GL GL = null!;
    Shader _sh_Outline = null!;

    public GameObject? go_selected = null;
    //public MeshComponent? selectedMeshComp { get; private set; } = null;
    private Mesh? mesh_outlined = null;
    private Mesh? mesh_selectedLast = null;
    private UI.RectGizmo RectDebugOutline = null!;
    public SelectedGizmoMode selectedGizmoMode = SelectedGizmoMode.Position;
    public SelectedPositionGizmoMode selectedPositionMode;
    public SelectedRotationGizmoMode selectedRotationMode;
    public SelectedScaleGizmoMode selectedScaleMode;
    public bool isInteracting => selectedPositionMode != SelectedPositionGizmoMode.None
        || selectedPositionOverMode != SelectedPositionGizmoMode.None;

    private const float _squareSize = 0.025f;
    private const float _axisLength = 0.15f;
    private const float _axisRadius = 0.005f;
    private const float _outlineWidth = 0.2f;

    public Vector3 selectedDragPos;
    public Quaternion selectedDragRot;
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

    public const string NormalMatrix = "uNormalMatrix";
    public const string NormalOffset = "uNormalOffset";


    public void Draw () {
        if (go_selected is null) return;

        Transform tr_obj = go_selected.Transform;
        Matrix4x4.Invert(Renderer.Instance.m4x4_View, out Matrix4x4 invView);
        Vector3 camPos = invView.Translation;
        Vector3 _objPos = tr_obj.Position;
        Quaternion _objRot = tr_obj.Rotation;
        float _dist = Vector3.Distance(camPos, _objPos);
        float half = 0.5f*_squareSize;

        if (Inputs.Actions[Inputs.GizmoLocal].pressedDown) {
            selectedGizmoWorldSpace = !selectedGizmoWorldSpace;
        }

        bool rayTrue = Camera.Main.GetRayMouse(out Ray ray);

        switch (selectedGizmoMode) {
            case SelectedGizmoMode.Position:
                if (selectedPositionMode == SelectedPositionGizmoMode.None) {
                    gizmoRight = selectedGizmoWorldSpace ? Vector3.UnitX : tr_obj.Right;
                    gizmoUp = selectedGizmoWorldSpace ? Vector3.UnitY : tr_obj.Up;
                    gizmoForward = selectedGizmoWorldSpace ? Vector3.UnitZ : tr_obj.Forward;
                }
                selectedPositionOverMode = SelectedPositionGizmoMode.None;

                Vector3 axisCapsuleXOffset = tr_obj.Position + gizmoRight*0.5f*_dist*_axisLength;
                Vector3 axisCapsuleYOffset = tr_obj.Position + gizmoUp*0.5f*_dist*_axisLength;
                Vector3 axisCapsuleZOffset = tr_obj.Position + gizmoForward*0.5f*_dist*_axisLength;

                /// Planes — always recomputed, independent of ray validity
                Vector3 toCam = camPos - _objPos;
                float signRight = Vector3.Dot(toCam, gizmoRight) < 0 ? -1 : 1;
                float signUp = Vector3.Dot(toCam, gizmoUp) < 0 ? -1 : 1;
                float signFront = Vector3.Dot(toCam, gizmoForward) < 0 ? -1 : 1;

                Vector3 quadXYOffset = signRight*gizmoRight*0.5f + signUp*gizmoUp*0.5f;
                Vector3 quadXZOffset = signRight*gizmoRight*0.5f + signFront*gizmoForward*0.5f;
                Vector3 quadYZOffset = signUp*gizmoUp*0.5f + signFront*gizmoForward*0.5f;

                /// Plane XY, Normal = forward
                quadXYBasis = BasisToWorld(gizmoRight, gizmoForward, gizmoUp);
                /// Plane XZ, Normal = up
                quadXZBasis = BasisToWorld(gizmoRight, gizmoUp, gizmoForward);
                /// Plane YZ, Normal = right
                quadYZBasis = BasisToWorld(gizmoUp, gizmoRight, gizmoForward);

                quadScale = _dist*_squareSize*Vector3.One;
                quadXYPos = _objPos + _dist*_squareSize*quadXYOffset;
                quadXZPos = _objPos + _dist*_squareSize*quadXZOffset;
                quadYZPos = _objPos + _dist*_squareSize*quadYZOffset;

                /// Ray-dependent picking/dragging only below this point
                if (!rayTrue) {
                    if (selectedPositionMode != SelectedPositionGizmoMode.None && !Inputs.Actions[Inputs.LMB].pressed) {
                        selectedPositionMode = SelectedPositionGizmoMode.None;
                        if (isMouseBlocked) {
                            isMouseBlocked = false;
                            CameraEditor.Instance?.UnblockMouse(this);
                        }
                    }
                    break;
                }

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
                            go_selected.Transform.Stop();
                            go_selected.Transform.Position = pos.Value + selectedDragMargin;
                            go_selected.Transform.Rotation = selectedDragRot;
                        }
                    }
                } else if (Inputs.Actions[Inputs.LMB].pressedUp) {
                    /// Release
                    selectedPositionMode = SelectedPositionGizmoMode.None;
                    if (isMouseBlocked) {
                        isMouseBlocked = false;
                        CameraEditor.Instance?.UnblockMouse(this);
                    }
                }

                break;
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

        /// Draw
        GL.Viewport(0, 0, (uint)RendererEditor.Instance.Width, (uint)RendererEditor.Instance.Height);

        Renderer.GL.Disable(EnableCap.DepthTest);
        Renderer.GL.Disable(EnableCap.CullFace);

        Shader _sh_Unlit = AssetsEngine._sh_Unlit;
        _sh_Unlit.Use();
        _sh_Unlit.SetMatrix4x4(View, Renderer.Instance.m4x4_View);
        _sh_Unlit.SetMatrix4x4(Projection, Renderer.Instance.m4x4_Projection);
        _sh_Unlit.SetVector3(ViewPos, Camera.Main.CameraPos);

        DrawOutline();
        DrawGizmo();
    }

    private void DrawGizmo () {
        if (go_selected is null) return;

        Transform tr_obj = go_selected.Transform;
        Matrix4x4.Invert(Renderer.Instance.m4x4_View, out Matrix4x4 invView);
        Vector3 camPos = invView.Translation;
        Vector3 _objPos = tr_obj.Position;
        Vector3 _objRot = tr_obj.LocalEuler;
        float _dist = Vector3.Distance(camPos, _objPos);
        bool isColorSelected;


        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);

        Shader _sh_Unlit = AssetsEngine._sh_Unlit;
        _sh_Unlit.Use();
        _sh_Unlit.SetMatrix4x4(View, Renderer.Instance.m4x4_View);
        _sh_Unlit.SetMatrix4x4(Projection, Renderer.Instance.m4x4_Projection);

        /// Quads
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.XY || selectedPositionOverMode == SelectedPositionGizmoMode.XY;
        drawQuad(quadXYPos, quadXYBasis, selectedPositionOverMode == SelectedPositionGizmoMode.XY ? Constants.blueLight : Constants.blue);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.XZ || selectedPositionOverMode == SelectedPositionGizmoMode.XZ;
        drawQuad(quadXZPos, quadXZBasis, selectedPositionOverMode == SelectedPositionGizmoMode.XZ ? Constants.greenLight : Constants.green);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.YZ || selectedPositionOverMode == SelectedPositionGizmoMode.YZ;
        drawQuad(quadYZPos, quadYZBasis, selectedPositionOverMode == SelectedPositionGizmoMode.YZ ? Constants.redLight : Constants.red);

        void drawQuad (Vector3 pos, Matrix4x4 basis, Vector3 color) {
            Matrix4x4 m4x4_selected = Matrix4x4.CreateScale(quadScale)*basis*Matrix4x4.CreateTranslation(pos);
            _sh_Unlit.SetMatrix4x4(Model, m4x4_selected);
            _sh_Unlit.SetVector3(Color, color);
            _sh_Unlit.SetFloat(Alpha, 0.5f);
            AssetsEngine._mesh_PlaneQuad.Draw();
        }

        /// Axes
        Matrix4x4 gizmoBasis = BasisToWorld(gizmoRight, gizmoUp, gizmoForward);
        Vector3 pos3 = go_selected.Transform.Position;
        Matrix4x4 _m4x4_selectedScale = Matrix4x4.CreateScale(_dist*_axisLength);

        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.X || selectedPositionOverMode == SelectedPositionGizmoMode.X;
        drawArrow(new Vector3(0, 90, 0), isColorSelected ? Constants.redLight : Constants.red);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.Y || selectedPositionOverMode == SelectedPositionGizmoMode.Y;
        drawArrow(new Vector3(-90, 0, 0), isColorSelected ? Constants.greenLight : Constants.green);
        isColorSelected = selectedPositionMode == SelectedPositionGizmoMode.Z || selectedPositionOverMode == SelectedPositionGizmoMode.Z;
        drawArrow(Vector3.Zero, isColorSelected ? Constants.blueLight : Constants.blue);

        void drawArrow (Vector3 rot, Vector3 color) {
            Matrix4x4 m4x4_selected = _m4x4_selectedScale*rot.EulerToMatrix()*gizmoBasis*Matrix4x4.CreateTranslation(pos3);
            _sh_Unlit.SetMatrix4x4(Model, m4x4_selected);
            _sh_Unlit.SetVector3(Color, color);
            _sh_Unlit.SetFloat(Alpha, 0.5f);
            Gizmos._mesh_Arrow3D.Draw();
        }
    }

    private void DrawOutline () {
        if (go_selected is null) return;
        MeshComponent? meshComp = go_selected.GetComponent<MeshComponent>();
        if (meshComp is null) {
            Engine.Graphics.UI.RectTransform? rect = go_selected.GetComponent<Engine.Graphics.UI.RectTransform>();
            if (rect is not null)
                RectDebugOutline.Draw(rect, Renderer.Instance.Width, Renderer.Instance.Height);
            return;
        }
        if (meshComp.mesh is null) return;
        if (mesh_outlined is null) return;

        RenderInfo renderInfo = meshComp.renderInfo;

        try {
            GL.Enable(EnableCap.StencilTest);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            /// Pass 1 - write object into stencil only
            GL.ColorMask(false, false, false, false);

            GL.DepthMask(false);
            GL.DepthFunc(DepthFunction.Always);

            GL.CullFace(TriangleFace.Back);

            GL.StencilMask(0xFF);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

            Renderer.Instance.DrawRenderInfo(renderInfo);


            /// Pass 2 - outline
            GL.ColorMask(true, true, true, true);

            GL.DepthMask(false);
            GL.DepthFunc(DepthFunction.Lequal);

            GL.CullFace(TriangleFace.Front);

            GL.StencilMask(0x00);
            GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);

            Matrix4x4.Invert(Renderer.Instance.m4x4_View, out Matrix4x4 invView);
            float dist = Vector3.Distance(invView.Translation, renderInfo.model.Translation);

            //Matrix4x4.Decompose(renderInfo.model, out _, out Quaternion rotation, out Vector3 position);
            //Matrix4x4 outlineModel = Matrix4x4.CreateFromQuaternion(rotation)*Matrix4x4.CreateTranslation(position);

            _sh_Outline.Use();
            _sh_Outline.SetMatrix4x4(View, Renderer.Instance.m4x4_View);
            _sh_Outline.SetMatrix4x4(Projection, Renderer.Instance.m4x4_Projection);
            _sh_Outline.SetMatrix4x4(Model, renderInfo.model);
            _sh_Outline.SetFloat(NormalOffset, 0.01f*dist*_outlineWidth);
            _sh_Outline.SetVector3(Color, Constants.cyan);
            _sh_Outline.SetFloat(Alpha, 1f);
            mesh_outlined.Draw();
        } finally {
            GL.CullFace(TriangleFace.Back);

            GL.ColorMask(true, true, true, true);

            GL.DepthMask(true);
            GL.DepthFunc(DepthFunction.Less);

            GL.StencilMask(0xFF);

            GL.Disable(EnableCap.StencilTest);
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


    public void UpdateSelected (GameObject? go) {
        go_selected = go;
        if (go is null) return;

        MeshComponent? meshComp = go.GetComponent<MeshComponent>();

        if (meshComp is not null) go_selected = meshComp.gameObject;

        Mesh? mesh = meshComp?.mesh;
        if (mesh == mesh_selectedLast) return; // already built, nothing changed

        mesh_selectedLast = mesh;
        RebuildOutlineMesh(mesh);
    }

    private void RebuildOutlineMesh (Mesh? mesh) {
        mesh_outlined?.Dispose();
        mesh_outlined = null;

        if (mesh?.Data is null) return;

        MeshData data = mesh.Data.Weld();
        data.RecalculateOutlineNormals();
        mesh_outlined = new Mesh(data);
    }


    public void Dispose () {
        mesh_selectedLast?.Dispose();
        mesh_outlined?.Dispose();
    }

}