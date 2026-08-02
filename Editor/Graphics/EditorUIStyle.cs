using ImGuiNET;

namespace Editor.Graphics;


public static class EditorUIStyle {

    public static readonly Vector4 AccentColor = new Vector4(0.8f, 0.8f, 0.8f, 1);
    public static readonly Vector4 AmbientColor = new Vector4(0.15f, 0.15f, 0.15f, 1);
    public static readonly Vector4 BackgroundColor = new Vector4(0.05f, 0.05f, 0.05f, 1);


    public static void SetAccentColor () {
        ImGui.StyleColorsDark();
        ImGuiStylePtr style = ImGui.GetStyle();

        style.Colors[(int)ImGuiCol.Text] = new Vector4(0.9f, 0.9f, 0.9f, 1);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 1);

        style.Colors[(int)ImGuiCol.WindowBg] = BackgroundColor;
        style.Colors[(int)ImGuiCol.ChildBg] = BackgroundColor;
        style.Colors[(int)ImGuiCol.PopupBg] = AmbientColor;

        style.Colors[(int)ImGuiCol.Border] = AccentColor * 0.5f;
        style.Colors[(int)ImGuiCol.BorderShadow] = Vector4.Zero;

        style.Colors[(int)ImGuiCol.FrameBg] = AmbientColor;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = AccentColor * 0.5f;
        style.Colors[(int)ImGuiCol.FrameBgActive] = AccentColor * 0.7f;

        style.Colors[(int)ImGuiCol.TitleBg] = BackgroundColor;
        style.Colors[(int)ImGuiCol.TitleBgActive] = AmbientColor;
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = BackgroundColor;

        style.Colors[(int)ImGuiCol.MenuBarBg] = AmbientColor;

        style.Colors[(int)ImGuiCol.ScrollbarBg] = BackgroundColor;
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = AccentColor * 0.5f;
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = AccentColor * 0.75f;
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = AccentColor;

        style.Colors[(int)ImGuiCol.CheckMark] = AccentColor;

        style.Colors[(int)ImGuiCol.SliderGrab] = AccentColor * 0.8f;
        style.Colors[(int)ImGuiCol.SliderGrabActive] = AccentColor;

        style.Colors[(int)ImGuiCol.Button] = AmbientColor;
        style.Colors[(int)ImGuiCol.ButtonHovered] = AccentColor * 0.6f;
        style.Colors[(int)ImGuiCol.ButtonActive] = AccentColor * 0.8f;

        style.Colors[(int)ImGuiCol.Header] = AccentColor * 0.4f;
        style.Colors[(int)ImGuiCol.HeaderHovered] = AccentColor * 0.6f;
        style.Colors[(int)ImGuiCol.HeaderActive] = AccentColor * 0.8f;

        style.Colors[(int)ImGuiCol.Separator] = AccentColor * 0.4f;
        style.Colors[(int)ImGuiCol.SeparatorHovered] = AccentColor * 0.7f;
        style.Colors[(int)ImGuiCol.SeparatorActive] = AccentColor;

        style.Colors[(int)ImGuiCol.ResizeGrip] = AccentColor * 0.4f;
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = AccentColor * 0.7f;
        style.Colors[(int)ImGuiCol.ResizeGripActive] = AccentColor;

        style.Colors[(int)ImGuiCol.Tab] = AmbientColor;
        style.Colors[(int)ImGuiCol.TabHovered] = AccentColor * 0.5f;
        style.Colors[(int)ImGuiCol.TabActive] = AccentColor * 0.7f;
        style.Colors[(int)ImGuiCol.TabUnfocused] = BackgroundColor;
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = AmbientColor;

        style.Colors[(int)ImGuiCol.DockingPreview] = AccentColor;
        style.Colors[(int)ImGuiCol.DockingEmptyBg] = BackgroundColor;

        style.Colors[(int)ImGuiCol.PlotLines] = AccentColor;
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = AccentColor;
        style.Colors[(int)ImGuiCol.PlotHistogram] = AccentColor;
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = AccentColor;

        style.Colors[(int)ImGuiCol.TableHeaderBg] = AmbientColor;
        style.Colors[(int)ImGuiCol.TableBorderStrong] = AccentColor * 0.5f;
        style.Colors[(int)ImGuiCol.TableBorderLight] = AccentColor * 0.25f;
        style.Colors[(int)ImGuiCol.TableRowBg] = BackgroundColor;
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = AmbientColor;

        style.Colors[(int)ImGuiCol.TextSelectedBg] = AccentColor * 0.5f;
        style.Colors[(int)ImGuiCol.DragDropTarget] = AccentColor;

        style.Colors[(int)ImGuiCol.NavHighlight] = AccentColor;
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = AccentColor;
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0, 0, 0, 0.2f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0, 0, 0, 0.4f);
    }

}
