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
        UpdateCamera(Window, Engine.Instance.Input, 0);
    }

    public static Camera Instance = null!;

    private IWindow Window = null!;

    /// Values
    internal const float _cameraSpeed = 8f;
    internal const float _cameraSpeedShift = 15f;
    internal const float _sensetivityMultiplier = 0.01f;
    internal const float _sensetivity = 0.2f;

    internal const float _focusGlideSpeed = 8f;
    internal const float _clickDragThresholdPixels = 4f;

    internal const float _moveStartSpeedFactor = 1f;
    internal const float _moveRampUpTime = 1.5f;
    internal const float _moveOvershootSpeedFactor = 2f;
    internal const float _moveMaxHoldTime = 5f;

    internal const float _cameraFOV = 0.25f*MathF.PI;
    internal const float _cameraPlaneClose = 0.1f;
    internal const float _cameraPlaneFar = 1000f;

    internal const float _zoomSpeed = 0.05f;


    /// Debug
    //private Matrix4X4<float> view;
    //private Matrix4X4<float> projection;

    internal Vector3D<float> cameraPos = new Vector3D<float>(-3, 2, -1);
    internal Matrix4X4<float> cameraRot = Matrix4X4<float>.Identity;
    internal Vector3D<float> cameraOrbitCenterPos = new Vector3D<float>(0, 0, 0);
    internal float yaw;
    internal float pitch;

    internal float previousMouseX;
    internal float previousMouseY;
    internal bool previousMmb;

    internal float mmbDownX;
    internal float mmbDownY;
    internal bool mmbDragged;

    internal bool isFocusing;
    internal Vector3D<float> focusTargetCameraPos;
    internal Vector3D<float> focusTargetOrbitCenterPos;

    //private Vector3D<float> previousMoveDirection = Vector3D<float>.Zero;
    private float moveHoldTime;


    internal void Update (IWindow Window, IInputContext Input, double deltaTime) {
        var keyboard = Input.Keyboards.FirstOrDefault();
        var mouse = Input.Mice.FirstOrDefault();
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

            bool flip = false;
            float flipSign = MathF.Cos(pitch) < 0 ? -1f : 1f;
            if (flip) flipSign = MathF.Cos(pitch) < 0 ? -1f : 1f;
            else flipSign = 1f;
            yaw += -dx*_sensetivityMultiplier*_sensetivity*flipSign;
            pitch += -dy*_sensetivityMultiplier*_sensetivity;
            pitch = Utils.WrapAngle(pitch);

            isFocusing = false;
        }

        UpdateCamera(Window, Input, deltaTime);


        /// Middle Mouse: drag to pan, clean click (no drag) to focus
        if (mmb && !previousMmb) {
            /// Just pressed
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

            cameraPosDelta = posDeltaL*dragSpeed*(-right*dx + Vector3D<float>.UnitY*dy);
            cameraPos += cameraPosDelta;
            cameraOrbitCenterPos += cameraPosDelta;
        } else if (!mmb && previousMmb) {
            /// Just released
            if (!mmbDragged) {
                TryFocusOnPoint(mouseX, mouseY, Window.Size.X, Window.Size.Y);
            }
        } else {

        }

        previousMmb = mmb;

        /// Smoothly glide toward the focus target, if focusing
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

        /// Move (speed ramps up the longer the same direction is held)
        float baseSpeed = keyboard.IsKeyPressed(Key.ShiftLeft) ? _cameraSpeedShift : _cameraSpeed;
        cameraPosDelta = Vector3D<float>.Zero;
        if (keyboard.IsKeyPressed(Key.W))
            cameraPosDelta += forward;
        if (keyboard.IsKeyPressed(Key.S))
            cameraPosDelta += -forward;
        if (keyboard.IsKeyPressed(Key.D))
            cameraPosDelta += right;
        if (keyboard.IsKeyPressed(Key.A))
            cameraPosDelta += -right;
        if (keyboard.IsKeyPressed(Key.Space) || keyboard.IsKeyPressed(Key.E))
            cameraPosDelta += up;
        if (keyboard.IsKeyPressed(Key.C) || keyboard.IsKeyPressed(Key.Q))
            cameraPosDelta += -up;

        if (0.0001f < cameraPosDelta.LengthSquared) {
            Vector3D<float> moveDirection = Vector3D.Normalize(cameraPosDelta);

            //bool sameDirection = 0.999f < Vector3D.Dot(moveDirection, previousMoveDirection);
            bool continuousMovement = moveDirection != Vector3D<float>.Zero;
            moveHoldTime = continuousMovement ? moveHoldTime + dt : 0f;
            //previousMoveDirection = moveDirection;

            float rampT = Utils.Clamp(moveHoldTime / _moveRampUpTime, 0f, 1f);
            float speedFactor = Utils.Lerp(_moveStartSpeedFactor, 1f, rampT);

            /// Accelerating
            if (_moveRampUpTime < moveHoldTime) {
                float overshootT = Utils.Clamp((moveHoldTime - _moveRampUpTime) / (_moveMaxHoldTime - _moveRampUpTime), 0f, 1f);
                speedFactor = Utils.Lerp(1f, _moveOvershootSpeedFactor, overshootT);
            }

            float speed = baseSpeed*speedFactor;
            cameraPosDelta = moveDirection*speed*dt;
        } else {
            //previousMoveDirection = Vector3D<float>.Zero;
            moveHoldTime = 0f;
        }

        cameraPos += cameraPosDelta;
        cameraOrbitCenterPos += cameraPosDelta;

        previousMouseX = mouseX;
        previousMouseY = mouseY;
    }

    internal void UpdateCamera (IWindow Window, IInputContext Input, double deltaTime) {
        var keyboard = Input.Keyboards.FirstOrDefault();
        var mouse = Input.Mice.FirstOrDefault();
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
            if (orbitDistance < 0.01f) orbitDistance = 5f; // fallback if center==pos initially

            Vector3D<float> referenceOffset = Vector3D<float>.UnitZ * orbitDistance; // (0,0,1)*dist, arbitrary baseline
            Vector3D<float> offset = Vector3D.Transform(referenceOffset, rotation);

            position = cameraOrbitCenterPos + offset;
            // forward is only needed for CreateLookAt below, compute it cleanly
            forward = Vector3D.Normalize(cameraOrbitCenterPos - position);
            // Don't touch rotation here — it's already correct
            cameraPos = position;

            mouse.Cursor.CursorMode = CursorMode.Raw;
        } else if (rmb) {
            /// Center Rotation
            rotation = Utils.CreateFromYawPitchRoll(yaw, pitch, 0f);
            forward = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = cameraPos;

            float orbitDistance = (cameraOrbitCenterPos - cameraPos).Length;
            if (orbitDistance < 0.01f) orbitDistance = 5f;
            cameraOrbitCenterPos = position + forward * orbitDistance;

            mouse.Cursor.CursorMode = CursorMode.Raw;
        } else {
            rotation = cameraRot;
            forward = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = cameraPos;

            mouse.Cursor.CursorMode = CursorMode.Normal;
        }

        Renderer.Instance.View = Matrix4X4.CreateLookAt(
            position,
            position + forward,
            worldUp
        );

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
            mouseX, mouseY, viewportWidth, viewportHeight, Renderer.Instance.View, Renderer.Instance.Projection);

        /// Fallback: ground plane at Y = 0.
        float? bestT = null;
        if (!bestT.HasValue) {
            float? planeT = Raycaster.IntersectPlane(
                rayOrigin, rayDirection, Vector3D<float>.Zero, Vector3D<float>.UnitY);
            if (planeT.HasValue) bestT = planeT;
        }

        if (!bestT.HasValue) return;

        Vector3D<float> hitPoint = rayOrigin + rayDirection*bestT.Value;

        /// Move camera closer
        const float targetDistance = 3f;
        Vector3D<float> currentForward = Vector3D.Transform(-Vector3D<float>.UnitZ, cameraRot);

        focusTargetOrbitCenterPos = hitPoint;
        focusTargetCameraPos = hitPoint - currentForward*targetDistance;
        isFocusing = true;
    }




}
