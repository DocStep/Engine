using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Graphics;
using Engine.Input;
using static Engine.Input.Inputs;

namespace Editor.Graphics;


public sealed class CameraEditor : Camera {
    public CameraEditor () : base() {
        Instance = this;
        SetTransformDefault();

        Renderer.Instance.de_LateUpdate += Update;

        priority = -10;
    }

    public static CameraEditor Instance = null!;

    public override string Name { get; } = nameof(CameraEditor);

    private float yaw;
    private float pitch;

    private Vector3 _cameraPos = new Vector3(-2, 3, -10);
    private Vector3 _cameraOrbitCenterPos = Vector3.Zero;

    public Vector3 _forward => Vector3.Transform(Vector3.UnitZ, cameraRot);
    Vector3 forward;
    Vector3 position;
    Vector3 worldUp;


    /// Values
    private const float _sensetivity = 0.1f;
    private const float _sensetivityMultiplier = 0.01f;

    private const float _cameraSpeed = 10f;
    private const float _cameraSpeedShift = 30f;

    private const float _focusGlideSpeed = 10f;
    private const float _clickDragThresholdPixels = 5f;
    private const float _snapThreshold = 0.001f;

    private const float _moveStartSpeedFactor = 1f;
    private const float _moveRampUpTime = 2f;
    private const float _moveOvershootSpeedFactor = 5f;
    private const float _moveMaxHoldTime = 10f;

    private const float _zoomSpeed = 0.1f;

    private const float _focusTargetDistanceMin = 0f;
    private const float _focusTargetDistanceMax = 100f;
    //private float _focusTargetDistance;

    internal Vector3 cameraOrbitCenterPos = Vector3.Zero;

    private float cameraDragStartX;
    private float cameraDragStartY;
    private bool isCameraDragging;
    //private bool previousMmb;

    private bool isFocusing;
    private Vector3 focusTargetCameraPos;
    private Vector3 focusTargetOrbitCenterPos;

    private float moveHoldTime;

    private List<object> mouseBlockingObjects = new List<object>();
    public bool mouseBlocked => 0 < mouseBlockingObjects.Count;
    public void BlockMouse (object obj) {
        if (!mouseBlockingObjects.Contains(obj))
            mouseBlockingObjects.Add(obj);
    }
    public void UnblockMouse (object obj) {
        if (mouseBlockingObjects.Contains(obj)) 
            mouseBlockingObjects.Remove(obj);
    }


    private void SetTransformDefault () {
        cameraPos = _cameraPos;
        cameraOrbitCenterPos = _cameraOrbitCenterPos;

        NewTransform();
    }
    private void SetTransformMaterialPreview () {
        cameraPos = _cameraPos;
        cameraOrbitCenterPos = new Vector3(0f, 0f, -8f);

        NewTransform();
    }
    private void NewTransform () {
        position = cameraPos;

        Vector3 dir = Vector3.Normalize(cameraOrbitCenterPos - cameraPos);
        yaw = -MathF.Atan2(dir.X, dir.Z);
        pitch = MathF.Asin(dir.Y);
        cameraRot = Utils.Matrix4x4FromYawPitchRoll(yaw, pitch, 0f);
        forward = Vector3.Transform(Vector3.UnitZ, cameraRot);
        UpdateWorldUp();

        isFocusing = false;
        moveHoldTime = 0f;
    }
    private void UpdateWorldUp () {
        worldUp = MathF.Cos(pitch) < 0 ? -Vector3.UnitY : Vector3.UnitY;
    }


    public override void Update () {
        //Log.log("Update", Inputs.isMouseVisible);
        float baseSpeed = Inputs.Actions[Shift].pressed ? _cameraSpeedShift : _cameraSpeed;
        Vector3 cameraPosDelta = Vector3.Zero;
        position = cameraPos;

        if (!EditorUI.Instance.isSceneUIHovered) {
            move();
            return;
        }

        cameraRot = Utils.Matrix4x4FromYawPitchRoll(yaw, pitch, 0f);
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, cameraRot);
        Vector3 right = Vector3.Transform(Vector3.UnitX, cameraRot);
        Vector3 up = Vector3.Transform(Vector3.UnitY, cameraRot);
        float posDeltaL = MathF.Max(0, (cameraOrbitCenterPos - cameraPos).Length());

        if ((Inputs.Actions[LMB].pressed && Inputs.Actions[Alt].pressed || Inputs.Actions[RMB].pressed) && !mouseBlocked) {
            float dx = Inputs.MouseDelta.X;
            float dy = Inputs.MouseDelta.Y;
            float flipSign = 1f;
            yaw += -dx*_sensetivityMultiplier*_sensetivity*flipSign;
            pitch += -dy*_sensetivityMultiplier*_sensetivity;
            pitch = Utils.WrapAngle(pitch);

            isFocusing = false;
        }
        

        if (Inputs.Actions[Alt].pressed && Inputs.Actions[LMB].pressed && !mouseBlocked) {
            /// Orbit Rotation
            cameraRot = Utils.Matrix4x4FromYawPitchRoll(yaw, pitch, 0f);

            float orbitDistance = (cameraPos - cameraOrbitCenterPos).Length();
            if (orbitDistance < 0.01f) orbitDistance = 5f;

            Vector3 offset = Vector3.Transform(Vector3.UnitZ * orbitDistance, cameraRot);
            position = cameraOrbitCenterPos - offset; /// was +offset
            forward = Vector3.Normalize(cameraOrbitCenterPos - position); /// unchanged, now correctly = +offset/|offset| = Transform(UnitZ, rotation) direction
            cameraPos = position;

            Inputs.MouseHide();
        } else if (Inputs.Actions[RMB].pressed && !mouseBlocked) {
            /// Center Rotation
            cameraRot = Utils.Matrix4x4FromYawPitchRoll(yaw, pitch, 0f);
            forward = Vector3.Transform(Vector3.UnitZ, cameraRot); /// was -UnitZ
            position = cameraPos;

            float orbitDistance = (cameraOrbitCenterPos - cameraPos).Length();
            if (orbitDistance < 0.01f) orbitDistance = 5f;
            cameraOrbitCenterPos = position + forward * orbitDistance;

            Inputs.MouseHide();
        } else if (Inputs.Actions[CameraDrag].pressedDown && !mouseBlocked) {
            /// Middle Mouse: drag to pan, clean click (no drag) to focus
            cameraDragStartX = Inputs.MousePos.X;
            cameraDragStartY = Inputs.MousePos.Y;
            isCameraDragging = false;
        } else if (Inputs.Actions[CameraDrag].pressed && !mouseBlocked) {
            const float dragSpeed = 0.001f;
            float dx = Inputs.MouseDelta.X;
            float dy = Inputs.MouseDelta.Y;

            float totalDx = Inputs.MousePos.X - cameraDragStartX;
            float totalDy = Inputs.MousePos.Y - cameraDragStartY;
            if (_clickDragThresholdPixels*_clickDragThresholdPixels < totalDx*totalDx + totalDy*totalDy) {
                isCameraDragging = true;
                isFocusing = false;
            }

            cameraPosDelta = posDeltaL*dragSpeed*(-right*dx + up*dy);
            cameraPos += cameraPosDelta;
            cameraOrbitCenterPos += cameraPosDelta;
            position = cameraPos;

            Inputs.MouseHide();
        } else if (Inputs.Actions[CameraDrag].pressedUp && !mouseBlocked) {
            if (!isCameraDragging) {
                TryFocusOnPoint(Inputs.MousePos.X, Inputs.MousePos.Y, Engine.Engine.Window.Size.X, Engine.Engine.Window.Size.Y);
            }
        } else {
            forward = Vector3.Transform(Vector3.UnitZ, cameraRot); /// was -UnitZ
            position = cameraPos;

            Inputs.MouseShow();
        }


        if (Inputs.Actions[Inputs.CameraFocus].pressedDown) 
            if (Gizmos._gizmo_Selected.selectedMeshComp is not null) 
                FocusAtPoint(Gizmos._gizmo_Selected.selectedMeshComp.owner.Transform.Position);

        /// Zoom
        if (InputState.WheelDelta != 0) {
            cameraPos += posDeltaL*_zoomSpeed*InputState.WheelDelta*forward;
            isFocusing = false;
        }

        if (Inputs.Actions[Reset].pressedDown) SetTransformDefault();
        if (Inputs.Actions[CameraFocusMaterial].pressedDown) SetTransformMaterialPreview();

        /// Select
        if (!EditorUI.Instance.isUIClick) {
            if (RaycastMouse(out Ray ray)) {
                if (Inputs.Actions[LMB].pressedDown && !Inputs.Actions[Alt].pressed && !Inputs.Actions[RMB].pressed) {
                    Raycast.RaycastSceneMesh(SceneManager.ActiveScene, ray, out MeshComponent? hitMeshComp, out Vector3 hitPos, out Vector3 hitNormal);
                    if (hitMeshComp is not null) {
                        Gizmos._gizmo_Selected.UpdateSelectedMesh(hitMeshComp);
                    } else {
                        Gizmos._gizmo_Selected.UpdateSelectedMesh(null);
                    }
                }
            }
        }

        /// Smoothly glide toward the focus target
        if (isFocusing) {
            Vector3 camDelta = focusTargetCameraPos - cameraPos;
            Vector3 orbitDelta = focusTargetOrbitCenterPos - cameraOrbitCenterPos;

            float _t = MathF.Min(1f, _focusGlideSpeed*(float)Time.deltaTime);
            cameraPos += camDelta*_t;
            cameraOrbitCenterPos += orbitDelta*_t;

            if (camDelta.LengthSquared() < _snapThreshold*_snapThreshold && orbitDelta.LengthSquared() < _snapThreshold*_snapThreshold) {
                cameraPos = focusTargetCameraPos;
                cameraOrbitCenterPos = focusTargetOrbitCenterPos;
                isFocusing = false;
            }
        }


        /// Move
        if (Inputs.Actions[MoveForward].pressed) cameraPosDelta += forward;
        if (Inputs.Actions[MoveBack].pressed) cameraPosDelta += -forward;
        if (Inputs.Actions[MoveRight].pressed) cameraPosDelta += right;
        if (Inputs.Actions[MoveLeft].pressed) cameraPosDelta += -right;
        if (Inputs.Actions[MoveUp].pressed) cameraPosDelta += up;
        if (Inputs.Actions[MoveDown].pressed) cameraPosDelta += -up;
        move();

        void move () {
            if (0.0001f < cameraPosDelta.LengthSquared()) {
                Vector3 moveDirection = Vector3.Normalize(cameraPosDelta);

                bool continuousMovement = moveDirection != Vector3.Zero;
                moveHoldTime = continuousMovement ? moveHoldTime + (float)Time.deltaTime : 0f;

                float rampT = Utils.Clamp(moveHoldTime / _moveRampUpTime, 0f, 1f);
                float speedFactor = Utils.Lerp(_moveStartSpeedFactor, 1f, rampT);

                if (_moveRampUpTime < moveHoldTime) {
                    float overshootT = Utils.Clamp(
                        (moveHoldTime - _moveRampUpTime)/(_moveMaxHoldTime - _moveRampUpTime), 0f, 1f);
                    speedFactor = Utils.Lerp(1f, _moveOvershootSpeedFactor, overshootT);
                }

                cameraPosDelta = moveDirection*baseSpeed*speedFactor*(float)Time.deltaTime;
                isFocusing = false;
            } else {
                moveHoldTime = 0f;
            }

            cameraPos += cameraPosDelta;
            cameraOrbitCenterPos += cameraPosDelta;
            UpdateWorldUp();
        }
    }


    public override Matrix4x4 GetViewMatrix () {
        return Matrix4x4.CreateLookAtLeftHanded(position, position + _forward, worldUp);
    }

    private void TryFocusOnPoint (float mouseX, float mouseY, int viewportWidth, int viewportHeight) {
        float? bestT = null;
        if (!RaycastMouse(out Ray ray)) return;

        Raycast.RaycastSceneMesh(SceneManager.ActiveScene, ray, out MeshComponent? hitMesh, out Vector3 hitPos, out Vector3 hitNormal);
        if (hitMesh is not null) {
            bestT = Vector3.Distance(cameraPos, hitPos);
        }

        /// Fallback: ground plane at Y = 0
        if (!bestT.HasValue) {
            float? planeT = Raycast.IntersectPlane(
                ray.Origin, ray.Direction, Vector3.Zero, Vector3.UnitY);
            if (planeT.HasValue) bestT = planeT;
        }
        if (!bestT.HasValue) return;

        Vector3 hitPoint = ray.Origin + ray.Direction * bestT.Value;

        FocusAtPoint(hitPoint);
    }

    public void FocusAtPoint (Vector3 pos) {
        float dist = Vector3.Distance(cameraPos, cameraOrbitCenterPos);
        dist = MathF.Max(_focusTargetDistanceMin, dist);
        dist = MathF.Min(dist, _focusTargetDistanceMax);
        focusTargetOrbitCenterPos = pos;
        focusTargetCameraPos = pos - _forward*dist;
        isFocusing = true;
    }


    public override bool RaycastMouse (out Ray ray) {
        //UI.TextRenderer.AddText($"MousePos_Window: {mousePos_Window.X}, {mousePos_Window.Y}");
        
        //Vector2? scenePos = mousePos;
        if (!EditorUI.Instance.GetSceneMousePos(Inputs.MousePos, out Vector2 scenePos)) {
            ray = default;
            return false;
        }

        Vector2 sceneSize = EditorUI.Instance.SceneAvail;

        Engine.Graphics.UI.TextRenderer.AddText($"MousePos_Scene: {scenePos.X}, {scenePos.Y}");
        Engine.Graphics.UI.TextRenderer.AddText($"SceneSize: {sceneSize.X}, {sceneSize.Y}");

        ray = Raycast.ScreenPointToRay(scenePos.X, scenePos.Y, (int)sceneSize.X, (int)sceneSize.Y,
            RendererEditor.Instance.m4x4_View, RendererEditor.Instance.m4x4Projection);
        return true;
    }

    /*private void LookAtOrbitCenter () {
        Vector3 offset = cameraOrbitCenterPos - cameraPos;
        float dist = offset.Length();
        if (dist < 0.0001f) return;

        Vector3 forward = offset / dist;

        pitch = MathF.Asin(Utils.Clamp(forward.Y, -1f, 1f));
        float cosPitch = MathF.Cos(pitch);
        yaw = -MathF.Atan2(forward.X / cosPitch, forward.Z / cosPitch);

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
    }*/

}
