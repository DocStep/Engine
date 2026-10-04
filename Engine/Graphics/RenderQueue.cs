using System.Runtime.InteropServices;

namespace Engine.Graphics;


/// Collects RenderInfo for a frame, culls, sorts and exposes the sorted visible list.
/// Sorting works on int indices + ulong keys, never on the RenderInfo structs themselves.
public sealed class RenderQueue {

    private readonly List<RenderData> renderInfos = new List<RenderData>();
    private int[] indices = [];
    private ulong[] keys = [];

    /// Number of visible items after Build
    public int Count { get; private set; }

    /// i is the position in the sorted visible list, not in the raw list
    public ref readonly RenderData this[int i] => ref CollectionsMarshal.AsSpan(renderInfos)[indices[i]];

    /// Raw unsorted, unculled items — for debug passes like wireframe
    public IReadOnlyList<RenderData> Items => renderInfos;

    public void Add (RenderData info) {
        renderInfos.Add(info);
    }

    public void Clear () {
        renderInfos.Clear();
        Count = 0;
    }

    /// Cull + build keys + sort
    public void Build (in Frustum frustum, Vector3 camPos) {
        int total = renderInfos.Count;
        if (indices.Length < total) {
            indices = new int[total];
            keys = new ulong[total];
        }

        Span<RenderData> items = CollectionsMarshal.AsSpan(renderInfos);
        Count = 0;
        for (int i = 0; i < total; i++) {
            ref readonly RenderData info = ref items[i];
            if (info.mesh is null || info.material is null) continue;

            if (info.material.Pass != RenderPass.UI) {
                AABB worldAABB = info.mesh.LocalAABB.Transformed(info.model);
                if (!frustum.Intersects(worldAABB)) continue;
            } /// UI is screen-space, so the world-space frustum test doesn't apply

            indices[Count] = i;
            keys[Count] = MakeSortKey(info, camPos);
            Count++;
        }

        /// Sorts the keys and moves the indices along with them
        bool sorted = true;
        for (int i = 1; i < Count; i++) {
            if (keys[i] < keys[i - 1]) { sorted = false; break; }
        }
        if (!sorted) Array.Sort(keys, indices, 0, Count);
    }

    /// End (exclusive) of the run of identical mesh+material that starts at start
    public int RunEnd (int start) {
        ref readonly RenderData first = ref this[start];
        int end = start + 1;
        while (end < Count) {
            ref readonly RenderData next = ref this[end];
            if (!ReferenceEquals(next.mesh, first.mesh) || !ReferenceEquals(next.material, first.material)) break;
            end++;
        }
        return end;
    }

    /// Opaque/UI: pass 2 bits | shader 22 bits | material 20 bits | mesh 20 bits
    /// Transparent: pass 2 bits | inverted distance 32 bits (back-to-front, required for correct blending)
    /// Relies on RenderPass order Opaque < Transparent < UI and on Id fields in Shader/Material/Mesh
    static ulong MakeSortKey (in RenderData info, Vector3 camPos) {
        ulong pass = (ulong)info.material.Pass & 3UL;

        if (info.material.Pass == RenderPass.Transparent) {
            float d = Vector3.DistanceSquared(camPos, info.model.Translation);
            uint inv = ~BitConverter.SingleToUInt32Bits(d); /// positive floats order as uints; invert for far-first
            return pass << 62 | (ulong)inv << 30;
        }

        return pass << 62
            | ((ulong)info.material.shader.Id_Renderer & 0x3FFFFFUL) << 40
            | ((ulong)info.material.Id_Renderer & 0xFFFFFUL) << 20
            | ((ulong)info.mesh.Id_Renderer & 0xFFFFFUL);
    }

}
