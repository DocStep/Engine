using ImGuiNET;
using System;
using System.Numerics;

namespace Editor.Graphics;


public class EngineInfoTab : IEditorTab {

    public string Name { get; set; } = "Engine Info";
    public bool isActive { get; set; } = true;

    private static readonly string FpsGraphicId = "##FpsGraphic";

    public const int fpsSamplesCount = 1000;
    private readonly float[] _fpsSamples = new float[fpsSamplesCount];
    private int _fpsSampleIndex = 0;
    private bool _fpsSamplesFilled = false;
    const long GBBytes = 1L << 30;
    const long MBBytes = 1L << 20;
    //const long KBBytes = 1L << 10;

    private float[] _gcSamples = new float[200]; // Adjust size to match your fpsSamplesCount
    private int _gcSampleIndex = 0;
    private bool _gcSamplesFilled = false;
    private long _lastFrameTotalBytes = 0;


    public void Draw () {
        ImGui.Begin(Name);
        ImGui.BeginDisabled();

        EditorUI.DrawTabContext(this);

        DrawGraphics();

        EditorUI.DrawObject(Engine.Engine.Instance.Stats);
        int count = SceneManager.ActiveScene.GameObjects.Count;
        EditorUI.DrawVar("GameObjects", ref count);

        long totalBytes = GC.GetTotalAllocatedBytes();
        long gbRemainder = totalBytes % GBBytes;
        long mbRemainder = gbRemainder % MBBytes;
        //float gcGB = (float)totalBytes / GBBytes; // Total size expressed in GB (e.g., 1.5 GB)
        float gcMB = (float)gbRemainder / MBBytes; // Leftover MBs after subtracting full GBs
        //float gcKB = (float)mbRemainder / KBBytes; // Leftover KBs after subtracting full MBs
        //EditorUI.DrawVar("GC.Alloc (GB)", ref gcGB);
        EditorUI.DrawVar("GC.Alloc (MB)", ref gcMB);
        //EditorUI.DrawVar("GC.Alloc (KB)", ref gcKB);

        ImGui.EndDisabled();
        ImGui.End();
    }

    private void DrawGraphics () {
        // --- 1. DATA SAMPLING (FPS & GC) ---
        float fps = Time.FPS;
        float frameMs = (float)(Time.unscaledDeltaTime * 1000d);
        if (float.IsNaN(frameMs) || float.IsInfinity(frameMs)) frameMs = 0f;

        _fpsSamples[_fpsSampleIndex] = frameMs;
        _fpsSampleIndex = (_fpsSampleIndex + 1) % fpsSamplesCount;
        if (_fpsSampleIndex == 0) _fpsSamplesFilled = true;

        // Calculate memory allocated specifically during this frame
        long totalBytes = GC.GetAllocatedBytesForCurrentThread();
        long allocatedThisFrame = 0;
        if (_lastFrameTotalBytes != 0 && totalBytes >= _lastFrameTotalBytes) {
            allocatedThisFrame = totalBytes - _lastFrameTotalBytes;
        }
        _lastFrameTotalBytes = totalBytes;

        // Convert delta allocation to Megabytes
        float gcMbThisFrame = (float)allocatedThisFrame / (1L << 20);
        float currentTotalMb = (float)totalBytes / (1L << 20);

        _gcSamples[_gcSampleIndex] = gcMbThisFrame;
        _gcSampleIndex = (_gcSampleIndex + 1) % _gcSamples.Length; // Adjust array length reference if shared
        if (_gcSampleIndex == 0) _gcSamplesFilled = true;


        // --- 2. LAYOUT & UI BOUNDS ---
        Vector2 avail = ImGui.GetContentRegionAvail();
        // Height is set to 210f to cleanly hold two stacked graphs
        Vector2 containerSize = new Vector2(MathF.Max(150f, avail.X), 210f);
        Vector2 origin = ImGui.GetCursorScreenPos();

        ImGui.InvisibleButton(FpsGraphicId, containerSize);

        ImDrawListPtr draw = ImGui.GetWindowDrawList();
        Vector2 containerMax = origin + containerSize;
        Vector2 pad = new Vector2(12f, 10f);

        // Shared styling colors
        uint bg = ImGui.GetColorU32(new Vector4(0.10f, 0.10f, 0.10f, 1f));
        uint grid = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.05f));
        uint dim = ImGui.GetColorU32(new Vector4(0.62f, 0.62f, 0.62f, 1f));
        uint fpsColor = fps >= 55f ? ImGui.GetColorU32(new Vector4(0.25f, 0.78f, 0.48f, 1f)) :
                        fps >= 30f ? ImGui.GetColorU32(new Vector4(0.95f, 0.72f, 0.28f, 1f)) :
                                     ImGui.GetColorU32(new Vector4(0.95f, 0.32f, 0.30f, 1f));
        uint gcColor = ImGui.GetColorU32(new Vector4(0.30f, 0.65f, 1.00f, 1f)); // Clean Blue for GC

        // Draw background bounding box
        draw.AddRectFilled(origin, containerMax, bg, 4f);


        /// Graph 1: FPS
        Vector2 fpsGraphMin = origin + new Vector2(12f, 38f);
        Vector2 fpsGraphMax = origin + new Vector2(containerSize.X - 12f, 95f);

        draw.AddText(origin + pad, fpsColor, $"{fps:0} FPS");
        draw.AddText(origin + new Vector2(75f, 10f), dim, $"{frameMs:0.0} ms");

        float maxFrameMs = 33.3f;
        int fpsSampleCount = _fpsSamplesFilled ? fpsSamplesCount : _fpsSampleIndex;
        for (int i = 0; i < fpsSampleCount; i++)
            maxFrameMs = MathF.Max(maxFrameMs, _fpsSamples[i]);
        maxFrameMs = MathF.Ceiling(maxFrameMs / 10f) * 10f;

        // Grid lines for FPS
        for (int i = 1; i <= 2; i++) {
            float y = fpsGraphMin.Y + (fpsGraphMax.Y - fpsGraphMin.Y) * i / 3f;
            draw.AddLine(new Vector2(fpsGraphMin.X, y), new Vector2(fpsGraphMax.X, y), grid);
        }

        if (1 < fpsSampleCount) {
            Vector2 prev = Vector2.Zero;
            for (int i = 0; i < fpsSampleCount; i++) {
                int idx = (_fpsSampleIndex - fpsSampleCount + i + fpsSamplesCount) % fpsSamplesCount;
                float normalized = Math.Clamp(_fpsSamples[idx] / maxFrameMs, 0f, 1f);
                float x = fpsGraphMin.X + (fpsGraphMax.X - fpsGraphMin.X) * i / (fpsSampleCount - 1);
                float y = fpsGraphMax.Y - (fpsGraphMax.Y - fpsGraphMin.Y) * normalized;
                Vector2 current = new Vector2(x, y);
                if (0 < i) draw.AddLine(prev, current, fpsColor, 1.5f);
                prev = current;
            }
        }
        draw.AddText(new Vector2(fpsGraphMax.X - 54f, fpsGraphMin.Y - 26f), dim, $"{maxFrameMs:0} ms");

        /// Graph 2: GC
        Vector2 gcOrigin = origin + new Vector2(0f, 105f); // Shift vertical anchor downwards
        Vector2 gcGraphMin = gcOrigin + new Vector2(12f, 38f);
        Vector2 gcGraphMax = gcOrigin + new Vector2(containerSize.X - 12f, 95f);

        draw.AddText(gcOrigin + pad, gcColor, "GC Delta");
        draw.AddText(gcOrigin + new Vector2(85f, 10f), dim, $"{gcMbThisFrame:0.00} MB/f");
        draw.AddText(gcOrigin + new Vector2(200f, 10f), dim, $"Total Thread: {currentTotalMb:0.0} MB");

        // Dynamic scale matching spikes in your allocations
        float maxGcMb = 0.5f; // Baseline 0.5 MB max window range
        int gcSampleCount = _gcSamplesFilled ? _gcSamples.Length : _gcSampleIndex;
        for (int i = 0; i < gcSampleCount; i++)
            maxGcMb = MathF.Max(maxGcMb, _gcSamples[i]);
        maxGcMb = MathF.Ceiling(maxGcMb * 10f) / 10f; // Round upwards smoothly

        // Grid lines for GC
        for (int i = 1; i <= 2; i++) {
            float y = gcGraphMin.Y + (gcGraphMax.Y - gcGraphMin.Y) * i / 3f;
            draw.AddLine(new Vector2(gcGraphMin.X, y), new Vector2(gcGraphMax.X, y), grid);
        }

        if (1 < gcSampleCount) {
            Vector2 prev = Vector2.Zero;
            for (int i = 0; i < gcSampleCount; i++) {
                int idx = (_gcSampleIndex - gcSampleCount + i + _gcSamples.Length) % _gcSamples.Length;
                float normalized = Math.Clamp(_gcSamples[idx] / maxGcMb, 0f, 1f);
                float x = gcGraphMin.X + (gcGraphMax.X - gcGraphMin.X) * i / (gcSampleCount - 1);
                float y = gcGraphMax.Y - (gcGraphMax.Y - gcGraphMin.Y) * normalized;
                Vector2 current = new Vector2(x, y);
                if (0 < i) draw.AddLine(prev, current, gcColor, 1.5f);
                prev = current;
            }
        }
        draw.AddText(new Vector2(gcGraphMax.X - 64f, gcGraphMin.Y - 26f), dim, $"{maxGcMb:0.00} MB");

        ImGui.Spacing();
    }

}
