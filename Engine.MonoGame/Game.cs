using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Engine.MonoGame;


public sealed class Game : Microsoft.Xna.Framework.Game {
    public Game () {
        _graphics = new GraphicsDeviceManager(this) {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = false,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        Window.Title = "Survival Engine";
        Window.AllowUserResizing = true;

        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 300.0);
        IsFixedTimeStep = true;
    }

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private WireframeCube? _skybox;
    private WorldGrid? _grid;
    private WorldAxes? _axes;
    private Cube? _cube;
    private Sphere? _sphere;

    private Matrix _view;
    private Matrix _projection;

    private Vector3 _cameraPos = new Vector3(1, 1, 2);
    private Matrix _cameraRot = Matrix.Identity;
    private Vector3 _cameraOrbitCenterPos = new Vector3(0, 0, 0);
    private float _yaw;
    private float _pitch;

    //private float _distanceOffset = 0f;

    private float _cameraSpeed = 10f;
    private float _cameraSpeedShift = 20f;
    private const float _sensetivityMultiplier = 0.01f;
    private float _sensetivity = 0.5f;

    private MouseState _previousMouse;
    private int _previousScroll;
    /*private CameraRotationMode _cameraRotationMode = CameraRotationMode.Center;
    public enum CameraRotationMode {
        Center,
        Orbital,
    }*/


    protected override void Initialize () {
        Window.ClientSizeChanged += OnResize;
        base.Initialize();
    }

    private void OnResize (object? sender, EventArgs e) {
        int w = Window.ClientBounds.Width;
        int h = Window.ClientBounds.Height;

        if (w <= 0 || h <= 0) return;

        _graphics.PreferredBackBufferWidth = w;
        _graphics.PreferredBackBufferHeight = h;
        _graphics.ApplyChanges();

        UpdateProjection();
    }

    private void UpdateProjection () {
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            GraphicsDevice.Viewport.AspectRatio,
            0.1f,
            100f);
    }

    protected override void LoadContent () {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _skybox = new WireframeCube(GraphicsDevice, 100f);
        _grid = new WorldGrid(GraphicsDevice, 10, 1);
        _axes = new WorldAxes(GraphicsDevice, 3f);
        _cube = new Cube(GraphicsDevice);
        _sphere = new Sphere(GraphicsDevice);

        UpdateProjection();

        _previousScroll = Mouse.GetState().ScrollWheelValue;

        LookAtOrbitCenter();
        UpdateCamera();
    }


    private void LookAtOrbitCenter () {
        Vector3 offset = _cameraPos - _cameraOrbitCenterPos;
        float dist = offset.Length();
        if (dist < 0.0001f) return;

        Vector3 forward = -offset / dist;

        _pitch = MathF.Asin(MathHelper.Clamp(forward.Y, -1f, 1f));
        float cosPitch = MathF.Cos(_pitch);
        _yaw = MathF.Atan2(-forward.X / cosPitch, -forward.Z / cosPitch);

        _cameraRot = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
    }

    protected override void Update (GameTime gameTime) {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        MouseState mouse = Mouse.GetState();
        KeyboardState kb = Keyboard.GetState();

        bool lmb = mouse.LeftButton == ButtonState.Pressed;
        bool rmb = mouse.RightButton == ButtonState.Pressed;
        bool mmb = mouse.MiddleButton == ButtonState.Pressed;
        bool alt = kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt);

        _cameraRot = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
        float posDeltaL = MathF.Max(0, (_cameraOrbitCenterPos - _cameraPos).Length());
        Vector3 forward = Vector3.Transform(Vector3.Forward, _cameraRot);
        Vector3 right = Vector3.Transform(Vector3.Right, _cameraRot);
        Vector3 up = Vector3.Transform(Vector3.Up, _cameraRot);
        Vector3 _cameraPosDelta = Vector3.Zero;

        if (alt && lmb || rmb) {
            /// RMB
            int dx = mouse.X - _previousMouse.X;
            int dy = mouse.Y - _previousMouse.Y;

            _yaw -= dx*_sensetivityMultiplier*_sensetivity;
            _pitch -= dy*_sensetivityMultiplier*_sensetivity;

            _pitch = MathHelper.Clamp(
                _pitch,
                -MathHelper.PiOver2 + 0.01f,
                MathHelper.PiOver2 - 0.01f);
        }

        UpdateCamera();

        /// Drag
        if (mmb && _previousMouse.MiddleButton == ButtonState.Pressed) {
            const float dragSpeed = 0.001f;
            int dx = mouse.X - _previousMouse.X;
            int dy = mouse.Y - _previousMouse.Y;

            _cameraPosDelta = posDeltaL*dragSpeed*(-right*dx + Vector3.Up*dy);
            _cameraPos += _cameraPosDelta;
            _cameraOrbitCenterPos += _cameraPosDelta;
        }

        /// Zoom
        int scrollDelta = mouse.ScrollWheelValue - _previousScroll;
        if (scrollDelta != 0) {
            //_distanceOffset -= 0.01f*scrollDelta;
            //_distanceOffset = MathHelper.Clamp(_distanceOffset, -10f, 10f);
            const float zoomSpeed = 0.001f;
            _cameraPos += posDeltaL*zoomSpeed*scrollDelta*forward;
        }
        _previousScroll = mouse.ScrollWheelValue;


        /// Move
        float speed = kb.IsKeyDown(Keys.LeftShift) ? _cameraSpeedShift : _cameraSpeed;
        _cameraPosDelta = Vector3.Zero;
        if (kb.IsKeyDown(Keys.W))
            _cameraPosDelta += forward;
        if (kb.IsKeyDown(Keys.S))
            _cameraPosDelta += -forward;
        if (kb.IsKeyDown(Keys.D))
            _cameraPosDelta += right;
        if (kb.IsKeyDown(Keys.A))
            _cameraPosDelta += -right;
        if (kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.E))
            _cameraPosDelta += up;
        if (kb.IsKeyDown(Keys.C) || kb.IsKeyDown(Keys.Q))
            _cameraPosDelta += -up;
        _cameraPosDelta *= speed*dt;

        _cameraPos += _cameraPosDelta;
        _cameraOrbitCenterPos += _cameraPosDelta;
        _previousMouse = mouse;

        base.Update(gameTime);
    }

    private void UpdateCamera () {
        MouseState mouse = Mouse.GetState();
        KeyboardState kb = Keyboard.GetState();

        bool lmb = mouse.LeftButton == ButtonState.Pressed;
        bool rmb = mouse.RightButton == ButtonState.Pressed;
        bool alt = kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt);

        Matrix rotation;
        Vector3 forward;
        Vector3 position;

        if (alt && lmb) {
            /// Orbit Rotation
            rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);

            // Always rotate a FIXED-LENGTH reference vector by the CURRENT
            // absolute angle — never re-rotate last frame's already-rotated offset.
            float orbitDistance = (_cameraPos - _cameraOrbitCenterPos).Length();
            if (orbitDistance < 0.01f) orbitDistance = 5f; // fallback if center==pos initially

            Vector3 referenceOffset = Vector3.Backward * orbitDistance; // (0,0,1)*dist, arbitrary baseline
            Vector3 offset = Vector3.Transform(referenceOffset, rotation);

            position = _cameraOrbitCenterPos + offset;
            forward = Vector3.Normalize(_cameraOrbitCenterPos - position);
            rotation = Matrix.CreateWorld(Vector3.Zero, forward, Vector3.Up);
            _cameraPos = position;
        } else if (rmb) {
            /// Center Rotation
            rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
            forward = Vector3.Transform(Vector3.Forward, rotation);
            position = _cameraPos;

            float orbitDistance = (_cameraOrbitCenterPos - _cameraPos).Length();
            if (orbitDistance < 0.01f) orbitDistance = 5f;
            _cameraOrbitCenterPos = position + forward * orbitDistance;
        } else {
            rotation = _cameraRot;
            forward = Vector3.Transform(Vector3.Forward, rotation);
            position = _cameraPos;
        }

        _view = Matrix.CreateLookAt(
            position,
            position + forward,
            Vector3.Up
        );

        _cameraRot = rotation;
    }

    protected override void Draw (GameTime gameTime) {
        GraphicsDevice.Clear(new Color(26, 26, 36));

        GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        GraphicsDevice.DepthStencilState = DepthStencilState.None;

        _skybox!.Draw(GraphicsDevice, _view, _projection, _cameraPos);
        _grid!.Draw(GraphicsDevice, _view, _projection);
        _axes!.Draw(GraphicsDevice, _view, _projection);

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        _sphere!.Draw(GraphicsDevice, Matrix.CreateScale(0.5f)*Matrix.CreateTranslation(_cameraOrbitCenterPos),
            _view, _projection, new Color(255, 255, 255, 64));
        GraphicsDevice.BlendState = BlendState.Opaque;

        _cube!.Draw(GraphicsDevice, Matrix.Identity, _view, _projection);

        _spriteBatch!.Begin();
        _spriteBatch!.End();

        base.Draw(gameTime);
    }

}