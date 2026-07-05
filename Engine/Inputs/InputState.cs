namespace Engine.Input;


public static class InputState {

    private static Silk.NET.Input.IKeyboard? keyboard;
    public static Silk.NET.Input.IKeyboard? Keyboard => keyboard;
    private static Silk.NET.Input.IMouse? mouse;
    public static Silk.NET.Input.IMouse? Mouse => mouse;

    public static Vector2 MousePos { get; private set; }
    public static Vector2 MouseDelta { get; private set; }
    public static float WheelDelta { get; private set; }

    private static HashSet<Keys> current = new HashSet<Keys>();
    private static HashSet<Keys> previous = new HashSet<Keys>();
    private static float wheelDelta = 0f;

    private static System.Numerics.Vector2 lastMousePos;
    private static bool firstMouseSample = true;


    public static void Init (Silk.NET.Input.IInputContext context) {
        /// Unsubscribe old scroll handler if Init is called again
        if (mouse is not null) mouse.Scroll -= OnScroll;

        keyboard = 0 < context.Keyboards.Count ? context.Keyboards[0] : null;
        mouse = 0 < context.Mice.Count ? context.Mice[0] : null;

        if (keyboard is null) Log.log("InputState.Init: No Keyboard");
        if (mouse is null) Log.log("InputState.Init: No Mouse");

        if (mouse is not null) mouse.Scroll += OnScroll;

        current.Clear();
        previous.Clear();
        wheelDelta = 0f;
        firstMouseSample = true;
    }

    static void OnScroll (Silk.NET.Input.IMouse mouse, Silk.NET.Input.ScrollWheel wheel) => wheelDelta = wheel.Y;

    public static void Update () {
        previous = new HashSet<Keys>(current);
        current.Clear();

        if (keyboard is not null) {
            foreach (Silk.NET.Input.Key key in System.Enum.GetValues<Silk.NET.Input.Key>()) {
                Keys mapped = InputKeyMap.FromKey(key);
                if (mapped == Keys.None) continue;
                if (keyboard.IsKeyPressed(key)) current.Add(mapped);
            }
        }

        //WheelDelta = 0f;
        if (mouse is not null) {
            foreach (Silk.NET.Input.MouseButton btn in System.Enum.GetValues<Silk.NET.Input.MouseButton>()) {
                Keys mapped = InputKeyMap.FromMouseButton(btn);
                if (mapped == Keys.None) continue;
                if (mouse.IsButtonPressed(btn)) current.Add(mapped);
            }

            if (0 < wheelDelta) current.Add(Keys.WheelUp);
            if (wheelDelta < 0) current.Add(Keys.WheelDown);
            WheelDelta = wheelDelta;

            PollMousePosition();
        }
        wheelDelta = 0f;
    }

    static void PollMousePosition () {
        MousePos = mouse!.Position;
        if (firstMouseSample) {
            lastMousePos = MousePos;
            firstMouseSample = false;
        }
        MouseDelta = new Vector2(MousePos.X - lastMousePos.X, MousePos.Y - lastMousePos.Y);
        lastMousePos = MousePos;
    }

    public static bool GetKeyDown (Keys k) => current.Contains(k) && !previous.Contains(k);
    public static bool GetKey (Keys k) => current.Contains(k);
    public static bool GetKeyUp (Keys k) => !current.Contains(k) && previous.Contains(k);

}