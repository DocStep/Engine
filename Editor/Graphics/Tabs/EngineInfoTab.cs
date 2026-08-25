using ImGuiNET;
using System;
using System.Numerics;

namespace Editor.Graphics;


public class EngineInfoTab : IEditorTab {

    public string Name { get; set; } = "Engine Info";
    public bool isActive { get; set; } = true;

    public const int fpsSamplesCount = 1000;
    private readonly float[] _fpsSamples = new float[fpsSamplesCount];
    private int _fpsSampleIndex = 0;
    private bool _fpsSamplesFilled = false;


    public void Draw () {
        ImGui.Begin(Name);
        ImGui.BeginDisabled();

        EditorUI.DrawTabContext(this);

        DrawFpsGraphic();


        EditorUI.DrawObject(Engine.Engine.Instance.Stats);

        ImGui.EndDisabled();
        ImGui.End();
    }

    private void DrawFpsGraphic () {
        float fps = Time.FPS;
        float frameMs = (float)(Time.unscaledDeltaTime * 1000d);
        if (float.IsNaN(frameMs) || float.IsInfinity(frameMs)) frameMs = 0f;

        _fpsSamples[_fpsSampleIndex] = frameMs;
        _fpsSampleIndex = (_fpsSampleIndex + 1) % fpsSamplesCount;
        if (_fpsSampleIndex == 0) _fpsSamplesFilled = true;

        Vector2 avail = ImGui.GetContentRegionAvail();
        Vector2 size = new Vector2(MathF.Max(150f, avail.X), 100f);
        Vector2 origin = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("##" + "FpsGraphic", size);

        ImDrawListPtr draw = ImGui.GetWindowDrawList();
        Vector2 max = origin + size;
        Vector2 pad = new Vector2(12f, 10f);
        Vector2 graphMin = origin + new Vector2(12f, 42f);
        Vector2 graphMax = max - new Vector2(12f, 12f);

        uint bg = ImGui.GetColorU32(new Vector4(0.10f, 0.10f, 0.10f, 1f));
        //uint border = ImGui.GetColorU32(EditorUIStyle.AccentColor * 0.35f);
        uint grid = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.08f));
        uint good = ImGui.GetColorU32(new Vector4(0.25f, 0.78f, 0.48f, 1f));
        uint warn = ImGui.GetColorU32(new Vector4(0.95f, 0.72f, 0.28f, 1f));
        uint bad = ImGui.GetColorU32(new Vector4(0.95f, 0.32f, 0.30f, 1f));
        uint dim = ImGui.GetColorU32(new Vector4(0.62f, 0.62f, 0.62f, 1f));

        draw.AddRectFilled(origin, max, bg, 0f);
        //draw.AddRect(origin, max, border, 0f);

        uint fpsColor = fps >= 55f ? good : fps >= 30f ? warn : bad;
        draw.AddText(origin + pad, fpsColor, $"{fps:0} FPS");
        draw.AddText(origin + new Vector2(90f, 12f), dim, $"{frameMs:0.00} ms");

        float maxFrameMs = 50f;
        int sampleCount = _fpsSamplesFilled ? fpsSamplesCount : _fpsSampleIndex;
        for (int i = 0; i < sampleCount; i++)
            maxFrameMs = MathF.Max(maxFrameMs, _fpsSamples[i]);
        maxFrameMs = MathF.Ceiling(maxFrameMs / 10f) * 10f;

        for (int i = 1; i <= 3; i++) {
            float y = graphMin.Y + (graphMax.Y - graphMin.Y) * i / 4f;
            draw.AddLine(new Vector2(graphMin.X, y), new Vector2(graphMax.X, y), grid);
        }

        if (1 < sampleCount) {
            Vector2 prev = Vector2.Zero;
            for (int i = 0; i < sampleCount; i++) {
                int sampleIndex = (_fpsSampleIndex - sampleCount + i + fpsSamplesCount) % fpsSamplesCount;
                float normalized = Math.Clamp(_fpsSamples[sampleIndex] / maxFrameMs, 0f, 1f);
                float x = graphMin.X + (graphMax.X - graphMin.X) * i / (sampleCount - 1);
                float y = graphMax.Y - (graphMax.Y - graphMin.Y) * normalized;
                Vector2 current = new Vector2(x, y);
                if (0 < i) draw.AddLine(prev, current, fpsColor, 2f);
                prev = current;
            }
        }

        draw.AddText(new Vector2(graphMin.X, graphMax.Y - 14f), dim, "frame time");
        draw.AddText(new Vector2(graphMax.X - 54f, graphMin.Y), dim, $"{maxFrameMs:0} ms");
        ImGui.Spacing();
    }

}
