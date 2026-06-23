namespace Engine.Input;


public enum Keys {
    None = 0,

    /// Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    /// Number row
    Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,

    /// Function
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    /// Control
    Escape, Tab, CapsLock, LeftShift, RightShift, LeftControl, RightControl,
    LeftAlt, RightAlt, Space, Enter, Backspace, Delete, Insert,
    Home, End, PageUp, PageDown,

    /// Arrows
    Up, Down, Left, Right,

    /// Numpad
    Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
    KeypadPeriod, KeypadDivide, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter, KeypadEquals,

    /// Symbols (US layout)
    Comma, Period, Slash, Semicolon, Quote, LeftBracket, RightBracket,
    Backslash, Minus, Equals, BackQuote,

    /// Mouse
    Mouse0, Mouse1, Mouse2, Mouse3, Mouse4,

    /// Wheel — treated as discrete keys, not deltas, so InputsGroup logic stays unchanged
    WheelUp, WheelDown,

    /// Misc
    Print
}
