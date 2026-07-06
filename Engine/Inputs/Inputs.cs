using System.Linq;

namespace Engine.Input;


public static class Inputs {

    public static void MouseShow () => InputState.Mouse?.Cursor.CursorMode = Silk.NET.Input.CursorMode.Raw;
    public static void MouseHide () => InputState.Mouse?.Cursor.CursorMode = Silk.NET.Input.CursorMode.Normal;

    public static Vector2 WASD = Vector2.Zero;
    public static Vector2 MousePos = Vector2.Zero;
    public static Vector2 MouseDelta = Vector2.Zero;
    public static float Wheel = 0;

    public static Dictionary<Keys, string> KeysNameOverride = new Dictionary<Keys, string>() {
        /// Mouse
        [Keys.Mouse0] = "LMB",
        [Keys.Mouse1] = "RMB",
        [Keys.Mouse2] = "WMB",

        /// Arrows
        [Keys.Up] = "Up",
        [Keys.Down] = "Down",
        [Keys.Left] = "Left",
        [Keys.Right] = "Right",

        /// Number row
        [Keys.Alpha0] = "0",
        [Keys.Alpha1] = "1",
        [Keys.Alpha2] = "2",
        [Keys.Alpha3] = "3",
        [Keys.Alpha4] = "4",
        [Keys.Alpha5] = "5",
        [Keys.Alpha6] = "6",
        [Keys.Alpha7] = "7",
        [Keys.Alpha8] = "8",
        [Keys.Alpha9] = "9",

        /// Command
        //[Keys.LeftShift] = "LShift",
        //[Keys.RightShift] = "RShift",
        //[Keys.LeftAlt] = "LAlt",
        //[Keys.RightAlt] = "RAlt",

        /// Symbols (US layout)
        [Keys.Comma] = ",",
        [Keys.Minus] = "-",
        [Keys.Period] = ".",
        [Keys.Slash] = "/",
        [Keys.Semicolon] = ";",
        [Keys.Equals] = "=",
        [Keys.LeftBracket] = "[",
        [Keys.Backslash] = "\\",
        [Keys.RightBracket] = "]",
        [Keys.BackQuote] = "`",
        [Keys.Quote] = "'",

        /// Numpad
        [Keys.Keypad0] = "Num0",
        [Keys.Keypad1] = "Num1",
        [Keys.Keypad2] = "Num2",
        [Keys.Keypad3] = "Num3",
        [Keys.Keypad4] = "Num4",
        [Keys.Keypad5] = "Num5",
        [Keys.Keypad6] = "Num6",
        [Keys.Keypad7] = "Num7",
        [Keys.Keypad8] = "Num8",
        [Keys.Keypad9] = "Num9",
        [Keys.KeypadPeriod] = "Num.",
        [Keys.KeypadDivide] = "Num/",
        [Keys.KeypadMultiply] = "Num*",
        [Keys.KeypadMinus] = "Num-",
        [Keys.KeypadPlus] = "Num+",
        [Keys.KeypadEnter] = "NumEnter",
        [Keys.KeypadEquals] = "Num=",

        /// Misc
        [Keys.Print] = "PrtScr",
        [Keys.WheelUp] = "WheelUp",
        [Keys.WheelDown] = "WheelDown",
    };
    public static readonly Dictionary<Keys, string> KeysName = InitKeyNames();
    public static readonly Dictionary<string, Keys> NameKeys = InitNamesKey();

    public const string LMB = nameof(LMB);
    public const string RMB = nameof(RMB);
    public const string WheelButton = nameof(WheelButton);
    public const string Mouse3 = nameof(Mouse3);
    public const string Mouse4 = nameof(Mouse4);
    public const string WheelUp = nameof(WheelUp);
    public const string WheelDown = nameof(WheelDown);

    public const string MoveForward = nameof(MoveForward);
    public const string MoveBack = nameof(MoveBack);
    public const string MoveRight = nameof(MoveRight);
    public const string MoveLeft = nameof(MoveLeft);
    public const string MoveUp = nameof(MoveUp);
    public const string MoveDown = nameof(MoveDown);

    public const string Jump = nameof(Jump);
    public const string Crouch = nameof(Crouch);
    public const string Prone = nameof(Prone);

    public const string _1 = "1";
    public const string _2 = "2";
    public const string _3 = "3";
    public const string _4 = "4";
    public const string _5 = "5";
    public const string _6 = "6";
    public const string _7 = "7";
    public const string _8 = "8";
    public const string _9 = "9";
    public const string _0 = "0";

    public const string Space = nameof(Space);
    public const string Shift = nameof(Shift);
    public const string Ctrl = nameof(Ctrl);
    public const string Alt = nameof(Alt);

    public const string NavForward = nameof(NavForward);
    public const string NavBack = nameof(NavBack);

    public const string CameraDrag = nameof(CameraDrag);
    public const string CameraFocus = nameof(CameraFocus);
    public const string CameraFocusMaterial = nameof(CameraFocusMaterial);
    public const string GizmoLocal = nameof(GizmoLocal);

    public const string F3 = nameof(F3);
    public const string Debug = nameof(Debug);
    public const string Reset = nameof(Reset);
    public const string EditorPause = nameof(EditorPause);
    public const string Temp = nameof(Temp);
    public const string EditorSave = nameof(EditorSave);


    public static Dictionary<string, InputsGroup> Actions = null!;
    public static Dictionary<string, InputsGroup> ActionsDef = new Dictionary<string, InputsGroup>() {
        [LMB] = new InputsGroup(new List<Keys>() { Keys.Mouse0 }, hidden: true),
        [RMB] = new InputsGroup(new List<Keys>() { Keys.Mouse1 }, hidden: true),
        [WheelButton] = new InputsGroup(new List<Keys>() { Keys.Mouse2 }, hidden: true),
        [Mouse3] = new InputsGroup(new List<Keys>() { Keys.Mouse3 }, hidden: true),
        [Mouse4] = new InputsGroup(new List<Keys>() { Keys.Mouse4 }, hidden: true),
        [WheelUp] = new InputsGroup(new List<Keys>() { Keys.WheelUp }, hidden: true),
        [WheelDown] = new InputsGroup(new List<Keys>() { Keys.WheelDown }, hidden: true),

        [_1] = new InputsGroup(new List<Keys>() { Keys.Alpha1 }),
        [_2] = new InputsGroup(new List<Keys>() { Keys.Alpha2 }),
        [_3] = new InputsGroup(new List<Keys>() { Keys.Alpha3 }),
        [_4] = new InputsGroup(new List<Keys>() { Keys.Alpha4 }),
        [_5] = new InputsGroup(new List<Keys>() { Keys.Alpha5 }),
        [_6] = new InputsGroup(new List<Keys>() { Keys.Alpha6 }),
        [_7] = new InputsGroup(new List<Keys>() { Keys.Alpha7 }),
        [_8] = new InputsGroup(new List<Keys>() { Keys.Alpha8 }),
        [_9] = new InputsGroup(new List<Keys>() { Keys.Alpha9 }),
        [_0] = new InputsGroup(new List<Keys>() { Keys.Alpha0 }),

        [MoveForward] = new InputsGroup(new List<Keys>() { Keys.W }),
        [MoveBack] = new InputsGroup(new List<Keys>() { Keys.S }),
        [MoveRight] = new InputsGroup(new List<Keys>() { Keys.D }),
        [MoveLeft] = new InputsGroup(new List<Keys>() { Keys.A }),
        [MoveUp] = new InputsGroup(new List<Keys>() { Keys.Space }),
        [MoveDown] = new InputsGroup(new List<Keys>() { Keys.C }),
        [Jump] = new InputsGroup(new List<Keys>() { Keys.Space }),
        [Crouch] = new InputsGroup(new List<Keys>() { Keys.C, Keys.LeftControl }),

        [Space] = new InputsGroup(new List<Keys>() { Keys.Space }, hidden: true),
        [Shift] = new InputsGroup(new List<Keys>() { Keys.LeftShift, Keys.RightShift }, hidden: true),
        [Ctrl] = new InputsGroup(new List<Keys>() { Keys.LeftControl, Keys.RightControl }, hidden: true),
        [Alt] = new InputsGroup(new List<Keys>() { Keys.LeftAlt, Keys.RightAlt }, hidden: true),

        [NavForward] = new InputsGroup(new List<Keys>() { Keys.Mouse4 }, hidden: true),
        [NavBack] = new InputsGroup(new List<Keys>() { Keys.Escape, Keys.Mouse3 }, hidden: true),

        [CameraDrag] = new([Keys.Mouse2], hidden: true),
        [CameraFocus] = new([Keys.F], hidden: true),
        [CameraFocusMaterial] = new([Keys.T], hidden: true),
        [GizmoLocal] = new([Keys.X], hidden: true),

        [F3] = new InputsGroup(new List<Keys>() { Keys.F3 }, hidden: true),
        [Debug] = new InputsGroup(new List<Keys>() { Keys.L }, hidden: true),
        [Reset] = new InputsGroup(new List<Keys>() { Keys.R }, hidden: true),
        [EditorPause] = new InputsGroup(new List<Keys>() { Keys.P }, hidden: true),
        [Temp] = new InputsGroup(new List<Keys>() { Keys.RightBracket }, hidden: true),
        [EditorSave] = new InputsGroup(new List<Keys>() { Keys.O }, hidden: true),
    };


    public static Dictionary<Keys, string> InitKeyNames () {
        Dictionary<Keys, string> keysNames = new Dictionary<Keys, string>();
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
            keysNames[key] = key.ToString();
        /// Override
        foreach (var itemKV in KeysNameOverride)
            keysNames[itemKV.Key] = itemKV.Value;
        return keysNames;
    }
    public static Dictionary<string, Keys> InitNamesKey () {
        Dictionary<string, Keys> keysNames = new Dictionary<string, Keys>();
        keysNames = KeysName.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        return keysNames;
    }


    public static string Key_Name (Keys key) {
        if (KeysName.ContainsKey(key)) return KeysName[key];
        else {
            Log.log($"Wrong Input Action Key {key}", Log.LogType.warning);
            return string.Empty;
        }
    }
    /// <summary> unsafe </summary>
    public static Keys Name_Key (string name) {
        if (NameKeys.ContainsKey(name)) return NameKeys[name];
        else {
            Log.log($"Wrong Input Action Name {name}", Log.LogType.warning);
            return Keys.None;
        }
    }


    public static void KeysInit () {
        Actions = null!;
        if (!KeysLoad()) KeysDef();
    }
    public static bool KeysLoad () {
        return false;
        if (!Directory.Exists(Engine.savesFolder)) return false;
        if (!File.Exists(getPathSettingsFile())) return false;


        Actions = new Dictionary<string, InputsGroup>(ActionsDef);

        StreamReader sr = new StreamReader(getPathSettingsFile());
        string[] lines = sr.ReadToEnd().Replace(" ", "").Split("\n");
        for (int l = 0; l < lines.Length; l++) {
            string[] parts = lines[l].Split(":");
            string act = parts[0];
            if (act.Length == 0) continue;
            if (act[0] == '[' || act[act.Length-1] == ']') continue;
            if (parts.Length < 2) continue;

            string[] keys = parts[1].Split(",");
            if (keys.Length == 0) continue;

            if (!Actions.TryGetValue(act, out InputsGroup? group)) continue;

            group.Keys.Clear();
            for (int k = 0; k < keys.Length; k++) {
                Keys key = Name_Key(keys[k]);
                if (key != Keys.None) group.Keys.Add(key);
            }
        }

        Log.log($"Inited (Loaded): {nameof(Inputs)} {getPathSettingsFile()}");
        return true;
    }
    public static void KeysDef () {
        Actions = new Dictionary<string, InputsGroup>(ActionsDef);
        Save();
        Log.log($"Inited (Created): {nameof(Inputs)}");
    }

    public static void AddActionsFirst (Dictionary<string, InputsGroup> keyset) {
        Actions = null!;
        Inputs.AddActions(keyset);
    }
    public static void AddActions (Dictionary<string, InputsGroup> keyset) {
        foreach (var actionKV in keyset) {
            ActionsDef.TryAdd(actionKV.Key, actionKV.Value);
        }
    }
    public static void OverrideActions (Dictionary<string, InputsGroup> keyset) {
        foreach (var actionKV in keyset) {
            if (ActionsDef.ContainsKey(actionKV.Key)) {
                ActionsDef[actionKV.Key] = actionKV.Value;
            } else ActionsDef.TryAdd(actionKV.Key, actionKV.Value);
        }
    }



    /// <summary> de_distance <-? </summary>
    public static void Update () {
        foreach (var actionKV in Actions) {
            InputsGroup group = actionKV.Value;
            group.pressedDown = false;
            group.pressed = false;
            group.pressedUp = false;
            for (int k = 0; k < group.Keys.Count; k++) {
                /// Wheel Key Exclusions — WheelUp/WheelDown are pulsed once per Update by InputState, not held
                if (group.Keys[k] == Keys.WheelUp) {
                    if (InputState.GetKey(Keys.WheelUp)) group.pressedDown = true;
                    continue;
                } else if (group.Keys[k] == Keys.WheelDown) {
                    if (InputState.GetKey(Keys.WheelDown)) group.pressedDown = true;
                    continue;
                }

                if (InputState.GetKeyDown(group.Keys[k])) group.pressedDown = true;
                if (InputState.GetKey(group.Keys[k])) group.pressed = true;
                if (InputState.GetKeyUp(group.Keys[k])) group.pressedUp = true;
            }
        }

        WASD.X = (Actions["MoveRight"].pressed ? 1 : 0) + (Actions["MoveLeft"].pressed ? -1 : 0);
        WASD.Y = (Actions["MoveForward"].pressed ? 1 : 0) + (Actions["MoveBack"].pressed ? -1 : 0);

        /// Mouse
        MousePos = InputState.MousePos;
        MouseDelta = InputState.MouseDelta;

        /// Wheel
        Wheel = InputState.WheelDelta;
    }


    /// <summary> FixedUpdate </summary>
    public static void End () {
        for (int a = 0; a < Actions.Count; a++) {
            Actions.ElementAt(a).Value.pressedDown = false;
            Actions.ElementAt(a).Value.pressed = false;
            Actions.ElementAt(a).Value.pressedUp = false;
        }
    }



    /// Rebind action from string (name of enum)
    public static void Rebind (string act, List<Keys> list) {
        if (Actions.ContainsKey(act)) {
            Actions[act] = new InputsGroup(list, ActionsDef[act].hidden);
        }
    }


    //static string pathSavesFolder = getPathSettingsFile();
    static string pathSavesFile = "Keybinds.txt";
    public static string getPathSettingsFile () => Path.Combine(Engine.savesFolder, pathSavesFile);


    public static void Save () {
        if (Actions is null) return;
        lib.DirectoryExists(Engine.savesFolder);

        string text = "";
        Group(Actions, "Keys");

        StreamWriter sw = new(getPathSettingsFile());
        sw.WriteLine(text);
        sw.Close();
        Log.log($"Keys saved");

        void Group (Dictionary<string, InputsGroup> group, string name) {
            text += $"[{name}]\n";
            foreach (var actsKV in group) {
                text += actsKV.Key + ": ";
                List<Keys> list = actsKV.Value.Keys;
                for (int k = 0; k < list.Count; k++) {
                    string keyName = Key_Name(list[k]);
                    if (k == 0) text += keyName;
                    else text += ", " + keyName;
                }
                text += "\n";
            }
            text += "\n";
        }
    }

}
