using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Engine.Graphics;

namespace Engine;


internal class Camera {
    internal Camera (IWindow Window) {
        Instance = this;
        this.Window = Window;

        var mouse = Engine.Instance.Input.Mice.FirstOrDefault();
        if (mouse is not null) {
            previousMouseX = mouse.Position.X;
            previousMouseY = mouse.Position.Y;
        }

        Window.Update += Update;

        UpdateCamera(0);
    }

    public static Camera Instance = null!;

    private IWindow Window = null!;

    /// Values
    private const float _cameraSpeed = 8f;
    private const float _cameraSpeedShift = 15f;
    private const float _sensetivityMultiplier = 0.01f;
    private const float _sensetivity = 0.2f;

    private const float _focusGlideSpeed = 8f;
    private const float _clickDragThresholdPixels = 4f;

    private const float _moveStartSpeedFactor = 1f;
    private const float _moveRampUpTime = 1.5f;
    private const float _moveOvershootSpeedFactor = 2f;
    private const float _moveMaxHoldTime = 5f;

    private const float _zoomSpeed = 0.1f;

    // How far the camera sits from the focus point after an MMB click focus
    private const float _focusTargetDistance = 3f;


    internal Vector3D<float> cameraPos = new Vector3D<float>(-3, 2, -1);
    internal Matrix4X4<float> cameraRot = Matrix4X4<float>.Identity;
    internal Vector3D<float> cameraOrbitCenterPos = new Vector3D<float>(0, 0, 0);

    private float yaw;
    private float pitch;

    private float previousMouseX;
    private float previousMouseY;
    private bool previousMmb;

    private float mmbDownX;
    private float mmbDownY;
    private bool mmbDragged;

    private bool isFocusing;
    private Vector3D<float> focusTargetCameraPos;
    private Vector3D<float> focusTargetOrbitCenterPos;

    private float moveHoldTime;


    private void Update (double deltaTime) {
        var keyboard = Engine.Instance.Input.Keyboards.FirstOrDefault();
        var mouse = Engine.Instance.Input.Mice.FirstOrDefault();
        if (keyboard == null || mouse == null) return;

        if (keyboard.IsKeyPressed(Key.Escape)) Window.Close();

        float dt = (float)deltaTime;
        float mouseX = mouse.Position.X;
        float mouseY = mouse.Position.Y;

        bool lmb = mouse.IsButtonPressed(MouseButton.Left);
        bool rmb = mouse.IsButtonPressed(MouseButton.Right);
        bool mmb = mouse.IsButtonPressed(MouseButton.Middle);
        bool alt = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);

        cameraRot = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
        float posDeltaL = MathF.Max(0, (cameraOrbitCenterPos - cameraPos).Length);
        Vector3D<float> forward = Vector3D.Transform(-Vector3D<float>.UnitZ, cameraRot);
        Vector3D<float> right = Vector3D.Transform(Vector3D<float>.UnitX, cameraRot);
        Vector3D<float> up = Vector3D.Transform(Vector3D<float>.UnitY, cameraRot);
        Vector3D<float> cameraPosDelta = Vector3D<float>.Zero;

        if (alt && lmb || rmb) {
            float dx = mouseX - previousMouseX;
            float dy = mouseY - previousMouseY;

            float flipSign = 1f;
            yaw   += -dx * _sensetivityMultiplier * _sensetivity * flipSign;
            pitch += -dy * _sensetivityMultiplier * _sensetivity;
            pitch = Utils.WrapAngle(pitch);

            isFocusing = false;
        }

        UpdateCamera(deltaTime);


        /// Middle Mouse: drag to pan, clean click (no drag) to focus
        if (mmb && !previousMmb) {
            mmbDownX = mouseX;
            mmbDownY = mouseY;
            mmbDragged = false;
        }

        if (mmb && previousMmb) {
            const float dragSpeed = 0.001f;
            float dx = mouseX - previousMouseX;
            float dy = mouseY - previousMouseY;

            float totalDx = mouseX - mmbDownX;
            float totalDy = mouseY - mmbDownY;
            if (totalDx*totalDx + totalDy*totalDy > _clickDragThresholdPixels*_clickDragThresholdPixels)
                mmbDragged = true;

            cameraPosDelta = posDeltaL * dragSpeed * (-right*dx + up*dy);
            cameraPos += cameraPosDelta;
            cameraOrbitCenterPos += cameraPosDelta;
        } else if (!mmb && previousMmb) {
            if (!mmbDragged) {
                TryFocusOnPoint(mouseX, mouseY, Window.Size.X, Window.Size.Y);
            }
        }

        previousMmb = mmb;

        /// Smoothly glide toward the focus target
        if (isFocusing) {
            Vector3D<float> camDelta = focusTargetCameraPos - cameraPos;
            Vector3D<float> orbitDelta = focusTargetOrbitCenterPos - cameraOrbitCenterPos;

            float t = MathF.Min(1f, _focusGlideSpeed*dt);
            cameraPos += camDelta*t;
            cameraOrbitCenterPos += orbitDelta*t;

            if (camDelta.Length < 0.01f && orbitDelta.Length < 0.01f) {
                cameraPos = focusTargetCameraPos;
                cameraOrbitCenterPos = focusTargetOrbitCenterPos;
                isFocusing = false;
            }
        }

        /// Zoom
        float scrollDelta = 0 < mouse.ScrollWheels.Count ? mouse.ScrollWheels[0].Y : 0;
        if (scrollDelta != 0) {
            cameraPos += posDeltaL*_zoomSpeed*scrollDelta*forward;
            isFocusing = false;
        }

        /// Move (speed ramps up the longer held)
        float baseSpeed = keyboard.IsKeyPressed(Key.ShiftLeft) ? _cameraSpeedShift : _cameraSpeed;
        cameraPosDelta = Vector3D<float>.Zero;
        if (keyboard.IsKeyPressed(Key.W)) cameraPosDelta +=  forward;
        if (keyboard.IsKeyPressed(Key.S)) cameraPosDelta += -forward;
        if (keyboard.IsKeyPressed(Key.D)) cameraPosDelta +=  right;
        if (keyboard.IsKeyPressed(Key.A)) cameraPosDelta += -right;
        if (keyboard.IsKeyPressed(Key.Space) || keyboard.IsKeyPressed(Key.E)) cameraPosDelta +=  up;
        if (keyboard.IsKeyPressed(Key.C) || keyboard.IsKeyPressed(Key.Q)) cameraPosDelta += -up;

        if (0.0001f < cameraPosDelta.LengthSquared) {
            Vector3D<float> moveDirection = Vector3D.Normalize(cameraPosDelta);

            bool continuousMovement = moveDirection != Vector3D<float>.Zero;
            moveHoldTime = continuousMovement ? moveHoldTime + dt : 0f;

            float rampT = Utils.Clamp(moveHoldTime / _moveRampUpTime, 0f, 1f);
            float speedFactor = Utils.Lerp(_moveStartSpeedFactor, 1f, rampT);

            if (_moveRampUpTime < moveHoldTime) {
                float overshootT = Utils.Clamp(
                    (moveHoldTime - _moveRampUpTime) / (_moveMaxHoldTime - _moveRampUpTime), 0f, 1f);
                speedFactor = Utils.Lerp(1f, _moveOvershootSpeedFactor, overshootT);
            }

            cameraPosDelta = moveDirection * baseSpeed * speedFactor * dt;
        } else {
            moveHoldTime = 0f;
        }

        cameraPos            += cameraPosDelta;
        cameraOrbitCenterPos += cameraPosDelta;

        previousMouseX = mouseX;
        previousMouseY = mouseY;
    }

    private void UpdateCamera (double deltaTime) {
        var keyboard = Engine.Instance.Input.Keyboards.FirstOrDefault();
        var mouse = Engine.Instance.Input.Mice.FirstOrDefault();
        if (keyboard == null || mouse == null) return;

        bool lmb = mouse.IsButtonPressed(MouseButton.Left);
        bool rmb = mouse.IsButtonPressed(MouseButton.Right);
        bool alt = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);

        Matrix4X4<float> rotation;
        Vector3D<float> forward;
        Vector3D<float> position;

        Vector3D<float> worldUp = MathF.Cos(pitch) < 0 ? -Vector3D<float>.UnitY : Vector3D<float>.UnitY;

        if (alt && lmb) {
            /// Orbit Rotation
            rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);

            float orbitDistance = (cameraPos - cameraOrbitCenterPos).Length;
            if (orbitDistance < 0.01f) orbitDistance = 5f;

            Vector3D<float> offset = Vector3D.Transform(Vector3D<float>.UnitZ * orbitDistance, rotation);
            position = cameraOrbitCenterPos + offset;
            forward  = Vector3D.Normalize(cameraOrbitCenterPos - position);
            cameraPos = position;

            mouse.Cursor.CursorMode = CursorMode.Raw;
        } else if (rmb) {
            /// Center Rotation
            rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
            forward  = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = cameraPos;

            float orbitDistance = (cameraOrbitCenterPos - cameraPos).Length;
            if (orbitDistance < 0.01f) orbitDistance = 5f;
            cameraOrbitCenterPos = position + forward * orbitDistance;

            mouse.Cursor.CursorMode = CursorMode.Raw;
        } else {
            rotation = cameraRot;
            forward  = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = cameraPos;

            mouse.Cursor.CursorMode = CursorMode.Normal;
        }

        Renderer.Instance.View = Matrix4X4.CreateLookAt(position, position + forward, worldUp);
        cameraRot = rotation;
    }


    internal void LookAtOrbitCenter () {
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
