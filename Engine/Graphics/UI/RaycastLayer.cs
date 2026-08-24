namespace Engine.Graphics.UI;


[Flags]
public enum RaycastLayer {
    None = 0,
    Default = 1<<0,
    UI = 1<<1,
    Tooltip = 1<<2,
    Blocker = 1<<3,
    All = ~0,
}
