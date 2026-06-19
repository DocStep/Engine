using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Engine;

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

    private Vector3 _cameraPos = new Vector3(0, 0, 1);
    private float _yaw;
    private float _pitch;

    private float _distanceOffset = 0f;
    private float _cameraSpeed = 2f;
    private float _cameraSpeedShift = 4f;
    private const float _sensetivityMultiplier = 0.01f;
    private float _sensetivity = 0.5f;


    private MouseState _previousMouse;
    private int _previousScroll;

    protected override void Initialize () {
        Window.ClientSizeChanged += OnResize;
        base.Initialize();
    }

    private void OnResize (object? sender, EventArgs e) {
        int w = Window.ClientBounds.Width;
        int h = Window.ClientBounds.Height;

        if (w <= 0 || h <= 0)
            return;

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
        _grid = new WorldGrid(GraphicsDevice, 1, 1);
        _axes = new WorldAxes(GraphicsDevice, 3f);
        _cube = new Cube(GraphicsDevice);
        _sphere = new Sphere(GraphicsDevice);

        UpdateProjection();

        _previousScroll = Mouse.GetState().ScrollWheelValue;

        UpdateCamera();
    }

    protected override void Update (GameTime gameTime) {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        MouseState mouse = Mouse.GetState();
        KeyboardState kb = Keyboard.GetState();

        bool rmb = mouse.RightButton == ButtonState.Pressed;
        bool mmb = mouse.MiddleButton == ButtonState.Pressed;

        int scrollDelta = mouse.ScrollWheelValue - _previousScroll;
        if (scrollDelta != 0) {
            _distanceOffset -= scrollDelta * 0.01f;
            _distanceOffset = MathHelper.Clamp(_distanceOffset, -10f, 10f);
        }
        _previousScroll = mouse.ScrollWheelValue;

        Matrix rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);

        Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);
        Vector3 right = Vector3.Transform(Vector3.Right, rotation);

        if (rmb && _previousMouse.RightButton == ButtonState.Pressed) {
            int dx = mouse.X - _previousMouse.X;
            int dy = mouse.Y - _previousMouse.Y;

            _yaw -= dx * _sensetivityMultiplier*_sensetivity;
            _pitch -= dy * _sensetivityMultiplier*_sensetivity;

            _pitch = MathHelper.Clamp(
                _pitch,
                -MathHelper.PiOver2 + 0.01f,
                MathHelper.PiOver2 - 0.01f);
        }

        if (mmb && _previousMouse.MiddleButton == ButtonState.Pressed) {
            int dx = mouse.X - _previousMouse.X;
            int dy = mouse.Y - _previousMouse.Y;

            const float panSpeed = 0.01f;

            _cameraPos += right * dx * panSpeed;
            _cameraPos += Vector3.Up * dy * panSpeed;
        }

        float speed = kb.IsKeyDown(Keys.LeftShift) ? _cameraSpeedShift : _cameraSpeed;

        if (kb.IsKeyDown(Keys.W))
            _cameraPos += forward * speed * dt;
        if (kb.IsKeyDown(Keys.S))
            _cameraPos -= forward * speed * dt;
        if (kb.IsKeyDown(Keys.D))
            _cameraPos += right * speed * dt;
        if (kb.IsKeyDown(Keys.A))
            _cameraPos -= right * speed * dt;

        _previousMouse = mouse;

        UpdateCamera();

        base.Update(gameTime);
    }

    private void UpdateCamera () {
        Matrix rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
        Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);
        Vector3 position = _cameraPos - forward * _distanceOffset;

        _view = Matrix.CreateLookAt(
            position,
            position + forward,
            Vector3.Up
        );
    }

    protected override void Draw (GameTime gameTime) {
        GraphicsDevice.Clear(new Color(26, 26, 36));

        GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        GraphicsDevice.DepthStencilState = DepthStencilState.None;

        _skybox!.Draw(GraphicsDevice, _view, _projection, _cameraPos);
        _grid!.Draw(GraphicsDevice, _view, _projection);
        _axes!.Draw(GraphicsDevice, _view, _projection);

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        _sphere!.Draw(GraphicsDevice, Matrix.Identity, _view, _projection);

        _spriteBatch!.Begin();
        _spriteBatch!.End();

        base.Draw(gameTime);
    }
}