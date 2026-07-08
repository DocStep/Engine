using Engine.Graphics;
using Engine.Input;
using static Engine.Input.Inputs;

namespace Engine.Graphics;


internal sealed class CameraEditor : Camera {
    internal CameraEditor () : base() {
        Instance = this;
        SetTransformDefault();
    }

    new public static CameraEditor? Instance = null;


    private Vector3 _cameraPos = new Vector3(-2, 3, -10);
    private Vector3 _cameraOrbitCenterPos = Vector3.Zero;

    /// Values
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
    public bool mouseAllowed => mouseBlockingObjects.Count == 0;
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
        cameraRot = Matrix4x4.CreateLookAtLeftHanded(cameraPos, cameraOrbitCenterPos, Vector3.UnitY);

        var dir = Vector3.Normalize(cameraOrbitCenterPos - cameraPos);
        yaw = -MathF.Atan2(dir.X, dir.Z);
        pitch = MathF.Asin(dir.Y);

        isFocusing = false;
        moveHoldTime = 0f;
    }


    protected override void Update (double deltaTime) {
        mousePos.X = Inputs.MousePos.X;
        mousePos.Y = Inputs.MousePos.Y;
        //Log.log($"mousePos {mousePos}");
        float dt = (float)deltaTime;

        if (Inputs.Actions[Reset].pressedDown) SetTransformDefault();
        if (Inputs.Actions[CameraFocusMaterial].pressedDown) SetTransformMaterialPreview();

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, cameraRot);
        Vector3 right = Vector3.Transform(Vector3.UnitX, cameraRot);
        Vector3 up = Vector3.Transform(Vector3.UnitY, cameraRot);
        Vector3 cameraPosDelta = Vector3.Zero;
        float posDeltaL = MathF.Max(0, (cameraOrbitCenterPos - cameraPos).Length());

        if ((Inputs.Actions[LMB].pressed && Inputs.Actions[Alt].pressed || Inputs.Actions[RMB].pressed) && mouseAllowed) {
            float dx = Inputs.MouseDelta.X;
            float dy = Inputs.MouseDelta.Y;

            float flipSign = 1f;
            yaw += -dx*_sensetivityMultiplier*_sensetivity*flipSign;
            pitch += -dy*_sensetivityMultiplier*_sensetivity;
            pitch = Utils.WrapAngle(pitch);

            isFocusing = false;
        }

        UpdateCamera(deltaTime);

        /// Middle Mouse: drag to pan, clean click (no drag) to focus
        if (Inputs.Actions[CameraDrag].pressedDown && mouseAllowed) {
            cameraDragStartX = mousePos.X;
            cameraDragStartY = mousePos.Y;
            isCameraDragging = false;
        }
        if (Inputs.Actions[CameraDrag].pressed) {
            const float dragSpeed = 0.001f;
            float dx = Inputs.MouseDelta.X;
            float dy = Inputs.MouseDelta.Y;

            float totalDx = mousePos.X - cameraDragStartX;
            float totalDy = mousePos.Y - cameraDragStartY;
            if (_clickDragThresholdPixels*_clickDragThresholdPixels < totalDx*totalDx + totalDy*totalDy) {
                isCameraDragging = true;
                isFocusing = false;
            }

            cameraPosDelta = posDeltaL*dragSpeed*(-right*dx + up*dy);
            cameraPos += cameraPosDelta;
            cameraOrbitCenterPos += cameraPosDelta;

            Inputs.MouseHide();
        } else if (Inputs.Actions[CameraDrag].pressedUp) {
            if (!isCameraDragging) {
                TryFocusOnPoint(mousePos.X, mousePos.Y, Engine.Window.Size.X, Engine.Window.Size.Y);
            }
        }

        if (Inputs.Actions[Inputs.CameraFocus].pressedDown && mouseAllowed) 
            if (Renderer.Instance._gizmo_Selected.selectedMesh is not null) 
                FocusAtPoint(Renderer.Instance._gizmo_Selected.selectedMesh.owner.Transform.Position);

        /// Smoothly glide toward the focus target
        if (isFocusing) {
            Vector3 camDelta = focusTargetCameraPos - cameraPos;
            Vector3 orbitDelta = focusTargetOrbitCenterPos - cameraOrbitCenterPos;

            float _t = MathF.Min(1f, _focusGlideSpeed*dt);
            cameraPos += camDelta*_t;
            cameraOrbitCenterPos += orbitDelta*_t;

            if (camDelta.LengthSquared() < _snapThreshold*_snapThreshold && orbitDelta.LengthSquared() < _snapThreshold*_snapThreshold) {
                cameraPos = focusTargetCameraPos;
                cameraOrbitCenterPos = focusTargetOrbitCenterPos;
                isFocusing = false;
            }
        }

        /// Zoom
        if (InputState.WheelDelta != 0) {
            cameraPos += posDeltaL*_zoomSpeed*InputState.WheelDelta*forward;
            isFocusing = false;
        }

        /// Move
        float baseSpeed = Inputs.Actions[Shift].pressed ? _cameraSpeedShift : _cameraSpeed;
        cameraPosDelta = Vector3.Zero;
        if (Inputs.Actions[MoveForward].pressed) cameraPosDelta += forward;
        if (Inputs.Actions[MoveBack].pressed) cameraPosDelta += -forward;
        if (Inputs.Actions[MoveRight].pressed) cameraPosDelta += right;
        if (Inputs.Actions[MoveLeft].pressed) cameraPosDelta += -right;
        if (Inputs.Actions[MoveUp].pressed) cameraPosDelta += up;
        if (Inputs.Actions[MoveDown].pressed) cameraPosDelta += -up;

        if (0.0001f < cameraPosDelta.LengthSquared()) {
            Vector3 moveDirection = Vector3.Normalize(cameraPosDelta);

            bool continuousMovement = moveDirection != Vector3.Zero;
            moveHoldTime = continuousMovement ? moveHoldTime + dt : 0f;

            float rampT = Utils.Clamp(moveHoldTime / _moveRampUpTime, 0f, 1f);
            float speedFactor = Utils.Lerp(_moveStartSpeedFactor, 1f, rampT);

            if (_moveRampUpTime < moveHoldTime) {
                float overshootT = Utils.Clamp(
                    (moveHoldTime - _moveRampUpTime)/(_moveMaxHoldTime - _moveRampUpTime), 0f, 1f);
                speedFactor = Utils.Lerp(1f, _moveOvershootSpeedFactor, overshootT);
            }

            cameraPosDelta = moveDirection * baseSpeed * speedFactor * dt;
            isFocusing = false;
        } else {
            moveHoldTime = 0f;
        }

        cameraPos += cameraPosDelta;
        cameraOrbitCenterPos += cameraPosDelta;
    }

    protected override void UpdateCamera (double deltaTime) {
        Matrix4x4 rotation;
        Vector3 forward;
        Vector3 position;
        Vector3 worldUp = MathF.Cos(pitch) < 0 ? -Vector3.UnitY : Vector3.UnitY;

        if (Inputs.Actions[Alt].pressed && Inputs.Actions[LMB].pressed) {
            /// Orbit Rotation
            rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);

            float orbitDistance = (cameraPos - cameraOrbitCenterPos).Length();
            if (orbitDistance < 0.01f) orbitDistance = 5f;

            Vector3 offset = Vector3.Transform(Vector3.UnitZ * orbitDistance, rotation);
            position = cameraOrbitCenterPos - offset; /// was +offset
            forward = Vector3.Normalize(cameraOrbitCenterPos - position); /// unchanged, now correctly = +offset/|offset| = Transform(UnitZ, rotation) direction
            cameraPos = position;

            Inputs.MouseShow();
        } else if (Inputs.Actions[RMB].pressed) {
            /// Center Rotation
            rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
            forward = Vector3.Transform(Vector3.UnitZ, rotation); /// was -UnitZ
            position = cameraPos;

            float orbitDistance = (cameraOrbitCenterPos - cameraPos).Length();
            if (orbitDistance < 0.01f) orbitDistance = 5f;
            cameraOrbitCenterPos = position + forward * orbitDistance;

            Inputs.MouseShow();
        } else {
            rotation = cameraRot;
            forward = Vector3.Transform(Vector3.UnitZ, rotation); /// was -UnitZ
            position = cameraPos;

            Inputs.MouseHide();
        }

        /// Select
        if (mouseAllowed && !EditorUI.Instance.isUIClick) {
            Ray ray = RaycastMouse();
            if (Inputs.Actions[LMB].pressedDown && !Inputs.Actions[Alt].pressed && !Inputs.Actions[RMB].pressed) {
                Raycast.RaycastSceneMesh(SceneManager.ActiveScene, ray, out MeshComponent? hitMesh, out Vector3 hitPos, out Vector3 hitNormal);
                if (hitMesh is not null) {
                    Renderer.Instance._gizmo_Selected.selectedMesh = hitMesh;
                } else {
                    Renderer.Instance._gizmo_Selected.selectedMesh = null;
                }
            }
        }
        
        Renderer.Instance.m4x4_View = Matrix4x4.CreateLookAtLeftHanded(position, position + forward, worldUp);
        cameraRot = rotation;
    }

    private void TryFocusOnPoint (float mouseX, float mouseY, int viewportWidth, int viewportHeight) {
        float? bestT = null;
        Ray ray = RaycastMouse();
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
        focusTargetCameraPos = pos - forward*dist;
        isFocusing = true;
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
