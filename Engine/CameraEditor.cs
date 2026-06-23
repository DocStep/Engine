using Silk.NET.Maths;
using Engine.Graphics;
using Engine.Input;
using static Engine.Input.Inputs;

namespace Engine;


internal sealed class CameraEditor : Camera {
    internal CameraEditor () : base() {
        Instance = this;
        SetTransformDefault();
        LookAtOrbitCenter();
    }

    new public static CameraEditor? Instance = null;


    private Vector3D<float> _cameraPos = new Vector3D<float>(-10, 3, -2);
    private Matrix4X4<float> _cameraRot = Matrix4X4<float>.Identity;
    private Vector3D<float> _cameraOrbitCenterPos = Vector3D<float>.Zero;

    /// Values
    private const float _cameraSpeed = 10f;
    private const float _cameraSpeedShift = 20f;

    private const float _focusGlideSpeed = 10f;
    private const float _clickDragThresholdPixels = 5f;

    private const float _moveStartSpeedFactor = 1f;
    private const float _moveRampUpTime = 2f;
    private const float _moveOvershootSpeedFactor = 5f;
    private const float _moveMaxHoldTime = 10f;

    private const float _zoomSpeed = 0.1f;

    private const float _focusTargetDistance = 3f;


    internal Vector3D<float> cameraOrbitCenterPos = Vector3D<float>.Zero;

    private float cameraDragStartX;
    private float cameraDragStartY;
    private bool isCameraDragging;
    //private bool previousMmb;

    private bool isFocusing;
    private Vector3D<float> focusTargetCameraPos;
    private Vector3D<float> focusTargetOrbitCenterPos;

    private float moveHoldTime;


    private void SetTransformDefault () {
        cameraPos = _cameraPos;
        cameraOrbitCenterPos = _cameraOrbitCenterPos;

        NewTransform();
    }
    private void SetTransformT () {
        cameraPos = _cameraPos;
        cameraOrbitCenterPos = new Vector3D<float>(-8f, 0f, 0f);

        NewTransform();
    }
    private void NewTransform () {
        cameraRot = Matrix4X4.CreateLookAt(cameraPos, cameraOrbitCenterPos, Vector3D<float>.UnitY);

        var dir = Vector3D.Normalize(cameraPos - cameraOrbitCenterPos);
        yaw = MathF.Atan2(dir.X, dir.Z);
        pitch = -MathF.Asin(dir.Y);

        isFocusing = false;
        moveHoldTime = 0f;
    }


    protected override void Update (double deltaTime) {
        if (Inputs.Actions[NavBack].pressedDown) Engine.Window.Close();

        mousePos.X = Inputs.MousePos.X;
        mousePos.Y = Inputs.MousePos.Y;
        //Log.log($"mousePos {mousePos}");
        float dt = (float)deltaTime;

        if (Inputs.Actions[Reset].pressedDown) SetTransformDefault();
        if (Inputs.Actions[CameraFocusMaterial].pressedDown) SetTransformT();

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        Vector3D<float> forward = Vector3D.Transform(-Vector3D<float>.UnitZ, cameraRot);
        Vector3D<float> right = Vector3D.Transform(Vector3D<float>.UnitX, cameraRot);
        Vector3D<float> up = Vector3D.Transform(Vector3D<float>.UnitY, cameraRot);
        Vector3D<float> cameraPosDelta = Vector3D<float>.Zero;
        float posDeltaL = MathF.Max(0, (cameraOrbitCenterPos - cameraPos).Length);

        if (Inputs.Actions[LMB].pressed && Inputs.Actions[Alt].pressed || Inputs.Actions[RMB].pressed) {
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
        if (Inputs.Actions[CameraDrag].pressedDown) {
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

        /// Smoothly glide toward the focus target
        if (isFocusing) {
            Vector3D<float> camDelta = focusTargetCameraPos - cameraPos;
            Vector3D<float> orbitDelta = focusTargetOrbitCenterPos - cameraOrbitCenterPos;

            float _t = MathF.Min(1f, _focusGlideSpeed*dt);
            cameraPos += camDelta*_t;
            cameraOrbitCenterPos += orbitDelta*_t;

            if (camDelta.Length < 0.01f && orbitDelta.Length < 0.01f) {
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
        float baseSpeed = Inputs.Actions[Shift].pressedDown ? _cameraSpeedShift : _cameraSpeed;
        cameraPosDelta = Vector3D<float>.Zero;
        if (Inputs.Actions[MoveForward].pressed) cameraPosDelta += forward;
        if (Inputs.Actions[MoveBack].pressed) cameraPosDelta += -forward;
        if (Inputs.Actions[MoveRight].pressed) cameraPosDelta += right;
        if (Inputs.Actions[MoveLeft].pressed) cameraPosDelta += -right;
        if (Inputs.Actions[MoveUp].pressed) cameraPosDelta += up;
        if (Inputs.Actions[MoveDown].pressed) cameraPosDelta += -up;

        if (0.0001f < cameraPosDelta.LengthSquared) {
            Vector3D<float> moveDirection = Vector3D.Normalize(cameraPosDelta);

            bool continuousMovement = moveDirection != Vector3D<float>.Zero;
            moveHoldTime = continuousMovement ? moveHoldTime + dt : 0f;

            float rampT = Utils.Clamp(moveHoldTime / _moveRampUpTime, 0f, 1f);
            float speedFactor = Utils.Lerp(_moveStartSpeedFactor, 1f, rampT);

            if (_moveRampUpTime < moveHoldTime) {
                float overshootT = Utils.Clamp(
                    (moveHoldTime - _moveRampUpTime)/(_moveMaxHoldTime - _moveRampUpTime), 0f, 1f);
                speedFactor = Utils.Lerp(1f, _moveOvershootSpeedFactor, overshootT);
            }

            cameraPosDelta = moveDirection * baseSpeed * speedFactor * dt;
        } else {
            moveHoldTime = 0f;
        }

        cameraPos += cameraPosDelta;
        cameraOrbitCenterPos += cameraPosDelta;
    }

    protected override void UpdateCamera (double deltaTime) {
        Matrix4X4<float> rotation;
        Vector3D<float> forward;
        Vector3D<float> position;

        Vector3D<float> worldUp = MathF.Cos(pitch) < 0 ? -Vector3D<float>.UnitY : Vector3D<float>.UnitY;

        if (Inputs.Actions[Alt].pressed && Inputs.Actions[LMB].pressed) {
            /// Orbit Rotation
            rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);

            float orbitDistance = (cameraPos - cameraOrbitCenterPos).Length;
            if (orbitDistance < 0.01f) orbitDistance = 5f;

            Vector3D<float> offset = Vector3D.Transform(Vector3D<float>.UnitZ * orbitDistance, rotation);
            position = cameraOrbitCenterPos + offset;
            forward  = Vector3D.Normalize(cameraOrbitCenterPos - position);
            cameraPos = position;

            Inputs.MouseShow();
        } else if (Inputs.Actions[RMB].pressed) {
            /// Center Rotation
            rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
            forward  = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = cameraPos;

            float orbitDistance = (cameraOrbitCenterPos - cameraPos).Length;
            if (orbitDistance < 0.01f) orbitDistance = 5f;
            cameraOrbitCenterPos = position + forward * orbitDistance;

            Inputs.MouseShow();
        } else {
            rotation = cameraRot;
            forward  = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = cameraPos;

            Inputs.MouseHide();
        }

        Renderer.Instance.View = Matrix4X4.CreateLookAt(position, position + forward, worldUp);
        cameraRot = rotation;
    }


    private void LookAtOrbitCenter () {
        Vector3D<float> offset = cameraPos - cameraOrbitCenterPos;
        float dist = offset.Length;
        if (dist < 0.0001f) return;

        Vector3D<float> forward = -offset / dist;

        pitch = MathF.Asin(Utils.Clamp(forward.Y, -1f, 1f));
        float cosPitch = MathF.Cos(pitch);
        yaw = MathF.Atan2(-forward.X / cosPitch, -forward.Z / cosPitch);

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
    }
    private void TryFocusOnPoint (float mouseX, float mouseY, int viewportWidth, int viewportHeight) {
        var (rayOrigin, rayDirection) = Raycaster.ScreenPointToRay(
            mouseX, mouseY, viewportWidth, viewportHeight,
            Renderer.Instance.View, Renderer.Instance.Projection);

        float? bestT = null;

        // Per-triangle raycast against all registered scene objects (BVH-accelerated)
        foreach (var obj in Scene.Objects) {
            float? t = obj.BVH.Intersect(rayOrigin, rayDirection, obj.Vertices, obj.Indices, obj.ModelMatrix);
            if (t.HasValue && (!bestT.HasValue || t.Value < bestT.Value))
                bestT = t;
        }

        // Fallback: ground plane at Y = 0
        if (!bestT.HasValue) {
            float? planeT = Raycaster.IntersectPlane(
                rayOrigin, rayDirection, Vector3D<float>.Zero, Vector3D<float>.UnitY);
            if (planeT.HasValue) bestT = planeT;
        }

        if (!bestT.HasValue) return;

        Vector3D<float> hitPoint = rayOrigin + rayDirection * bestT.Value;
        Vector3D<float> currentForward = Vector3D.Transform(-Vector3D<float>.UnitZ, cameraRot);

        focusTargetOrbitCenterPos = hitPoint;
        focusTargetCameraPos = hitPoint - currentForward*_focusTargetDistance;
        isFocusing = true;
    }


}
