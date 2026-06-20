using System.Linq;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.SilkNet;


public class Game : IDisposable {
    private IWindow _window = null!;
    private GL _gl = null!;
    private IInputContext _input = null!;

    private Cube _cube = null!;
    private Sphere _sphere = null!;
    private Sphere _gizmoSphere = null!;
    private WorldGrid _grid = null!;
    private WorldAxes _axes = null!;
    private WorldAxes _gizmoAxes = null!;

    private Shader _litShader = null!;
    private Shader _unlitShader = null!;
    private Shader _axesShader = null!;

    private Matrix4X4<float> _view;
    private Matrix4X4<float> _projection;

    private Vector3D<float> _cameraPos = new Vector3D<float>(1, 1, 2);
    private Matrix4X4<float> _cameraRot = Matrix4X4<float>.Identity;
    private Vector3D<float> _cameraOrbitCenterPos = new Vector3D<float>(0, 0, 0);
    private float _yaw;
    private float _pitch;

    private float _cameraSpeed = 10f;
    private float _cameraSpeedShift = 20f;
    private const float _sensetivityMultiplier = 0.01f;
    private float _sensetivity = 0.2f;

    private float _previousMouseX;
    private float _previousMouseY;
    private bool _previousMmb;

    private float _mmbDownX;
    private float _mmbDownY;
    private bool _mmbDragged;

    private bool _isFocusing;
    private Vector3D<float> _focusTargetCameraPos;
    private Vector3D<float> _focusTargetOrbitCenterPos;
    private const float _focusGlideSpeed = 8f;
    private const float _clickDragThresholdPixels = 4f;

    private Vector3D<float> _previousMoveDirection = Vector3D<float>.Zero;
    private float _moveHoldTime;
    private const float _moveStartSpeedFactor = 0.25f; 
    private const float _moveRampUpTime = 1.5f;
    private const float _moveOvershootSpeedFactor = 2f;
    private const float _moveMaxHoldTime = 5f;


    public void Run () {
        var options = WindowOptions.Default with {
            Size = new Vector2D<int>(1280, 720),
            Title = "Survival Engine",
            VSync = false,
        };

        _window = Window.Create(options);

        var monitors = Silk.NET.Windowing.Monitor.GetMonitors(_window);
        var monitor = monitors.First();
        var screenSize = monitor.VideoMode.Resolution ?? new Vector2D<int>(1920, 1080);
        _window.Position = new Vector2D<int>(
            (screenSize.X - options.Size.X)/2,
            (screenSize.Y - options.Size.Y)/2);

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnClosing;

        _window.Run();
    }

    private void OnLoad () {
        _input = _window.CreateInput();
        foreach (var keyboard in _input.Keyboards) {
            keyboard.KeyDown += OnKeyDown;
        }

        _gl = _window.CreateOpenGL();
        _gl.ClearColor(0.1f, 0.1f, 0.15f, 1f);
        _gl.Enable(EnableCap.DepthTest);

        _cube = new Cube(_gl);
        _sphere = new Sphere(_gl);
        _gizmoSphere = new Sphere(_gl);
        _grid = new WorldGrid(_gl, 10, 1f);
        _axes = new WorldAxes(_gl, 1000f);
        _gizmoAxes = new WorldAxes(_gl, 1f);

        _litShader = new Shader(_gl, LoadSrc("src/Vertex.shader"), LoadSrc("src/Fragment.shader"));
        _unlitShader = new Shader(_gl, LoadSrc("src/UnlitVertex.shader"), LoadSrc("src/UnlitFragment.shader"));
        _axesShader = new Shader(_gl, LoadSrc("src/AxesVertex.shader"), LoadSrc("src/AxesFragment.shader"));

        UpdateProjection();

        var mouse = _input.Mice.FirstOrDefault();
        if (mouse is not null) {
            _previousMouseX = mouse.Position.X;
            _previousMouseY = mouse.Position.Y;
        }

        LookAtOrbitCenter();
        UpdateCamera();
    }

    private void UpdateProjection () {
        float aspect = _window.Size.X / (float)_window.Size.Y;
        _projection = Matrix4X4.CreatePerspectiveFieldOfView(
            MathF.PI / 4f,
            aspect,
            0.1f,
            100f);
    }

    private void LookAtOrbitCenter () {
        Vector3D<float> offset = _cameraPos - _cameraOrbitCenterPos;
        float dist = offset.Length;
        if (dist < 0.0001f) return;

        Vector3D<float> forward = -offset / dist;

        _pitch = MathF.Asin(Clamp(forward.Y, -1f, 1f));
        float cosPitch = MathF.Cos(_pitch);
        _yaw = MathF.Atan2(-forward.X / cosPitch, -forward.Z / cosPitch);

        _cameraRot = CreateFromYawPitchRoll(_yaw, _pitch, 0f);
    }

    private void OnUpdate (double deltaTime) {
        var keyboard = _input.Keyboards.FirstOrDefault();
        var mouse = _input.Mice.FirstOrDefault();
        if (keyboard == null || mouse == null) return;

        if (keyboard.IsKeyPressed(Key.Escape)) _window.Close();

        float dt = (float)deltaTime;

        float mouseX = mouse.Position.X;
        float mouseY = mouse.Position.Y;

        bool lmb = mouse.IsButtonPressed(MouseButton.Left);
        bool rmb = mouse.IsButtonPressed(MouseButton.Right);
        bool mmb = mouse.IsButtonPressed(MouseButton.Middle);
        bool alt = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);

        _cameraRot = CreateFromYawPitchRoll(_yaw, _pitch, 0f);
        float posDeltaL = MathF.Max(0, (_cameraOrbitCenterPos - _cameraPos).Length);
        Vector3D<float> forward = Vector3D.Transform(-Vector3D<float>.UnitZ, _cameraRot);
        Vector3D<float> right = Vector3D.Transform(Vector3D<float>.UnitX, _cameraRot);
        Vector3D<float> up = Vector3D.Transform(Vector3D<float>.UnitY, _cameraRot);
        Vector3D<float> cameraPosDelta = Vector3D<float>.Zero;

        if (alt && lmb || rmb) {
            /// RMB / Alt+LMB look
            float dx = mouseX - _previousMouseX;
            float dy = mouseY - _previousMouseY;

            bool flip = false;
            float flipSign = MathF.Cos(_pitch) < 0 ? -1f : 1f;
            if (flip) flipSign = MathF.Cos(_pitch) < 0 ? -1f : 1f;
            else flipSign = 1f;
            _yaw += -dx*_sensetivityMultiplier*_sensetivity*flipSign;
            _pitch += -dy*_sensetivityMultiplier*_sensetivity;
            _pitch = WrapAngle(_pitch);

            _isFocusing = false;
        }

        UpdateCamera();

        /// Middle Mouse: drag to pan, clean click (no drag) to focus
        if (mmb && !_previousMmb) {
            /// Just pressed
            _mmbDownX = mouseX;
            _mmbDownY = mouseY;
            _mmbDragged = false;
        }

        if (mmb && _previousMmb) {
            const float dragSpeed = 0.001f;
            float dx = mouseX - _previousMouseX;
            float dy = mouseY - _previousMouseY;

            float totalDx = mouseX - _mmbDownX;
            float totalDy = mouseY - _mmbDownY;
            if (totalDx*totalDx + totalDy*totalDy > _clickDragThresholdPixels*_clickDragThresholdPixels)
                _mmbDragged = true;

            cameraPosDelta = posDeltaL*dragSpeed*(-right*dx + Vector3D<float>.UnitY*dy);
            _cameraPos += cameraPosDelta;
            _cameraOrbitCenterPos += cameraPosDelta;
        } else if (!mmb && _previousMmb) {
            /// Just released
            if (!_mmbDragged) {
                TryFocusOnPoint(mouseX, mouseY, _window.Size.X, _window.Size.Y);
            }
        } else {

        }

        _previousMmb = mmb;

        /// Smoothly glide toward the focus target, if focusing
        if (_isFocusing) {
            Vector3D<float> camDelta = _focusTargetCameraPos - _cameraPos;
            Vector3D<float> orbitDelta = _focusTargetOrbitCenterPos - _cameraOrbitCenterPos;

            float t = MathF.Min(1f, _focusGlideSpeed*dt);
            _cameraPos += camDelta*t;
            _cameraOrbitCenterPos += orbitDelta*t;

            if (camDelta.Length < 0.01f && orbitDelta.Length < 0.01f) {
                _cameraPos = _focusTargetCameraPos;
                _cameraOrbitCenterPos = _focusTargetOrbitCenterPos;
                _isFocusing = false;
            }
        }

        /// Zoom
        float scrollDelta = mouse.ScrollWheels.Count > 0 ? mouse.ScrollWheels[0].Y : 0f;
        if (scrollDelta != 0f) {
            const float zoomSpeed = 0.1f;
            _cameraPos += posDeltaL*zoomSpeed*scrollDelta*forward;
            _isFocusing = false;
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

            bool sameDirection = 0.999f < Vector3D.Dot(moveDirection, _previousMoveDirection);
            _moveHoldTime = sameDirection ? _moveHoldTime + dt : 0f;
            _previousMoveDirection = moveDirection;

            float rampT = Clamp(_moveHoldTime / _moveRampUpTime, 0f, 1f);
            float speedFactor = Lerp(_moveStartSpeedFactor, 1f, rampT);

            /// Accelerating
            if (_moveRampUpTime < _moveHoldTime) {
                float overshootT = Clamp((_moveHoldTime - _moveRampUpTime) / (_moveMaxHoldTime - _moveRampUpTime), 0f, 1f);
                speedFactor = Lerp(1f, _moveOvershootSpeedFactor, overshootT);
            }

            float speed = baseSpeed*speedFactor;
            cameraPosDelta = moveDirection*speed*dt;
        } else {
            _previousMoveDirection = Vector3D<float>.Zero;
            _moveHoldTime = 0f;
        }

        _cameraPos += cameraPosDelta;
        _cameraOrbitCenterPos += cameraPosDelta;

        _previousMouseX = mouseX;
        _previousMouseY = mouseY;
    }

    private void TryFocusOnPoint (float mouseX, float mouseY, int viewportWidth, int viewportHeight) {
        var (rayOrigin, rayDirection) = Raycaster.ScreenPointToRay(
            mouseX, mouseY, viewportWidth, viewportHeight, _view, _projection);

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
        Vector3D<float> currentForward = Vector3D.Transform(-Vector3D<float>.UnitZ, _cameraRot);

        _focusTargetOrbitCenterPos = hitPoint;
        _focusTargetCameraPos = hitPoint - currentForward*targetDistance;
        _isFocusing = true;
    }

    private void UpdateCamera () {
        var keyboard = _input.Keyboards.FirstOrDefault();
        var mouse = _input.Mice.FirstOrDefault();
        if (keyboard == null || mouse == null) return;

        bool lmb = mouse.IsButtonPressed(MouseButton.Left);
        bool rmb = mouse.IsButtonPressed(MouseButton.Right);
        bool alt = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);

        Matrix4X4<float> rotation;
        Vector3D<float> forward;
        Vector3D<float> position;

        Vector3D<float> worldUp = MathF.Cos(_pitch) < 0 ? -Vector3D<float>.UnitY : Vector3D<float>.UnitY;

        if (alt && lmb) {
            /// Orbit Rotation
            rotation = CreateFromYawPitchRoll(_yaw, _pitch, 0f);

            float orbitDistance = (_cameraPos - _cameraOrbitCenterPos).Length;
            if (orbitDistance < 0.01f) orbitDistance = 5f; // fallback if center==pos initially

            Vector3D<float> referenceOffset = Vector3D<float>.UnitZ * orbitDistance; // (0,0,1)*dist, arbitrary baseline
            Vector3D<float> offset = Vector3D.Transform(referenceOffset, rotation);

            position = _cameraOrbitCenterPos + offset;
            forward = Vector3D.Normalize(_cameraOrbitCenterPos - position);
            rotation = CreateWorld(forward, worldUp);
            _cameraPos = position;

            mouse.Cursor.CursorMode = CursorMode.Disabled;
        } else if (rmb) {
            /// Center Rotation
            rotation = CreateFromYawPitchRoll(_yaw, _pitch, 0f);
            forward = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = _cameraPos;

            float orbitDistance = (_cameraOrbitCenterPos - _cameraPos).Length;
            if (orbitDistance < 0.01f) orbitDistance = 5f;
            _cameraOrbitCenterPos = position + forward * orbitDistance;

            mouse.Cursor.CursorMode = CursorMode.Disabled;
        } else {
            rotation = _cameraRot;
            forward = Vector3D.Transform(-Vector3D<float>.UnitZ, rotation);
            position = _cameraPos;

            mouse.Cursor.CursorMode = CursorMode.Normal;
        }

        _view = Matrix4X4.CreateLookAt(
            position,
            position + forward,
            worldUp
        );

        _cameraRot = rotation;
    }

    private void OnRender (double deltaTime) {
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);

        Matrix4X4<float> modelIdentity = Matrix4X4<float>.Identity;

        /// Grid (unlit, opaque, gray)
        _unlitShader.Use();
        _unlitShader.SetMatrix4("uModel", MatrixToArray(modelIdentity));
        _unlitShader.SetMatrix4("uView", MatrixToArray(_view));
        _unlitShader.SetMatrix4("uProjection", MatrixToArray(_projection));
        _unlitShader.SetVector3("uColor", 0.35f, 0.35f, 0.4f);
        _unlitShader.SetFloat("uAlpha", 1f);
        _grid.Draw();

        /// Axes (vertex-colored, unlit, always on top of grid/scene)
        _gl.Disable(EnableCap.DepthTest);
        _axesShader.Use();
        _axesShader.SetMatrix4("uModel", MatrixToArray(modelIdentity));
        _axesShader.SetMatrix4("uView", MatrixToArray(_view));
        _axesShader.SetMatrix4("uProjection", MatrixToArray(_projection));
        _axes.Draw();
        _gl.Enable(EnableCap.DepthTest);


        /// Cube (lit, opaque, light gray)
        _litShader.Use();
        Vector3D<float> lightGray = new Vector3D<float>(0.78f, 0.78f, 0.78f);
        //Vector3D<float> lightDir = Vector3D.Normalize(new Vector3D<float>(-0.4f, -1f, -0.3f));
        Vector3D<float> lightDir = Vector3D.Normalize(new Vector3D<float>(0f, -1f, 0f));
        Vector3D<float> lightColor = new Vector3D<float>(1f, 1f, 1f);

        _litShader.SetMatrix4("uView", MatrixToArray(_view));
        _litShader.SetMatrix4("uProjection", MatrixToArray(_projection));
        _litShader.SetVector3("uLightDir", lightDir.X, lightDir.Y, lightDir.Z);
        _litShader.SetVector3("uLightColor", lightColor.X, lightColor.Y, lightColor.Z);
        _litShader.SetVector3("uViewPos", _cameraPos.X, _cameraPos.Y, _cameraPos.Z);

        Matrix4X4<float> cubeModel =
            //Matrix4X4.CreateRotationX(MathF.PI / 4f) *
            //Matrix4X4.CreateRotationY(MathF.PI / 4f) *
            Matrix4X4.CreateTranslation(new Vector3D<float>(0f, 0f, 0f));
        _litShader.SetMatrix4("uModel", MatrixToArray(cubeModel));
        _litShader.SetVector3("uColor", lightGray.X, lightGray.Y, lightGray.Z);
        _cube.Draw();

        Matrix4X4<float> sphereModel = Matrix4X4.CreateScale(0.5f)*Matrix4X4.CreateTranslation(new Vector3D<float>(1.5f, 0f, 0f));
        _litShader.SetMatrix4("uModel", MatrixToArray(sphereModel));
        _sphere.Draw();

        /// Sphere (unlit, semi-transparent, small pivot marker, centered on orbit center)
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.DepthMask(false);

        _unlitShader.Use();
        Matrix4X4<float> gizmoSphereModel = Matrix4X4.CreateScale(0.05f)*Matrix4X4.CreateTranslation(_cameraOrbitCenterPos);
        _unlitShader.SetMatrix4("uModel", MatrixToArray(gizmoSphereModel));
        _unlitShader.SetMatrix4("uView", MatrixToArray(_view));
        _unlitShader.SetMatrix4("uProjection", MatrixToArray(_projection));
        _unlitShader.SetVector3("uColor", 0f, 0f, 0f);
        _unlitShader.SetFloat("uAlpha", 0.2f);
        _gizmoSphere.Draw();

        _gl.DepthMask(true);
        _gl.Disable(EnableCap.Blend);

        DrawGizmo();
    }

    private void DrawGizmo () {
        const int gizmoSize = 90;
        const int gizmoMargin = 16;

        int windowWidth = _window.Size.X;
        int windowHeight = _window.Size.Y;

        int gizmoX = windowWidth - gizmoSize - gizmoMargin;
        int gizmoY = windowHeight - gizmoSize - gizmoMargin;

        _gl.Viewport(gizmoX, gizmoY, (uint)gizmoSize, (uint)gizmoSize);
        _gl.Clear(ClearBufferMask.DepthBufferBit);

        Matrix4X4<float> rotation = _cameraRot;

        Vector3D<float> forward = Vector3D.Transform(Vector3D<float>.UnitZ, rotation);
        Vector3D<float> up = Vector3D.Transform(Vector3D<float>.UnitY, rotation);
        Vector3D<float> gizmoCamPos = forward * 2.5f;
        Matrix4X4<float> gizmoView = Matrix4X4.CreateLookAt(
            gizmoCamPos,
            Vector3D<float>.Zero,
            up
        );

        Matrix4X4<float> gizmoProjection = Matrix4X4.CreateOrthographic(2.2f, 2.2f, 0.1f, 10f);

        _gl.Disable(EnableCap.DepthTest);

        _axesShader.Use();
        _axesShader.SetMatrix4("uModel", MatrixToArray(Matrix4X4<float>.Identity));
        _axesShader.SetMatrix4("uView", MatrixToArray(gizmoView));
        _axesShader.SetMatrix4("uProjection", MatrixToArray(gizmoProjection));

        _gizmoAxes.Draw();

        _gl.Enable(EnableCap.DepthTest);

        _gl.Viewport(_window.Size);
    }

    private void OnFramebufferResize (Vector2D<int> newSize) {
        _gl.Viewport(newSize);
        if (newSize.X > 0 && newSize.Y > 0)
            UpdateProjection();
    }

    private void OnKeyDown (IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            _window.Close();
        }
    }

    private void OnClosing () {
        _cube.Dispose();
        _sphere.Dispose();
        _gizmoSphere.Dispose();
        _grid.Dispose();
        _axes.Dispose();
        _gizmoAxes.Dispose();
        _litShader.Dispose();
        _unlitShader.Dispose();
        _axesShader.Dispose();
    }

    
    private static Matrix4X4<float> CreateFromYawPitchRoll (float yaw, float pitch, float roll) {
        return Matrix4X4.CreateRotationZ(roll)
             * Matrix4X4.CreateRotationX(pitch)
             * Matrix4X4.CreateRotationY(yaw);
    }

    private static Matrix4X4<float> CreateWorld (Vector3D<float> forward, Vector3D<float> up) {
        Vector3D<float> z = Vector3D.Normalize(-forward);
        Vector3D<float> x = Vector3D.Normalize(Vector3D.Cross(up, z));
        Vector3D<float> y = Vector3D.Cross(z, x);

        return new Matrix4X4<float>(
            x.X, x.Y, x.Z, 0f,
            y.X, y.Y, y.Z, 0f,
            z.X, z.Y, z.Z, 0f,
            0f, 0f, 0f, 1f);
    }

    /// Wraps an angle to [-pi, pi].
    private static float WrapAngle (float angle) {
        angle %= MathF.PI * 2f;
        if (angle > MathF.PI) angle -= MathF.PI * 2f;
        if (angle < -MathF.PI) angle += MathF.PI * 2f;
        return angle;
    }

    private static float Clamp (float value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static float Lerp (float a, float b, float t) => a + (b - a)*t;

    private static float[] MatrixToArray (Matrix4X4<float> m) => new[] {
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44,
    };

    private static string LoadSrc (string relativePath) {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}.");
        return File.ReadAllText(fullPath);
    }

    public void Dispose () {
        _window?.Dispose();
    }
}