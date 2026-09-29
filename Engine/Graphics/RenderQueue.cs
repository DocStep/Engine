using System.Runtime.InteropServices;

namespace Engine.Graphics;


/// Collects RenderInfo for a frame, culls, sorts and exposes the sorted visible list.
/// Sorting works on int indices + ulong keys, never on the RenderInfo structs themselves.
public sealed class RenderQueue {

    readonly List<RenderInfo> _items = new List<RenderInfo>();
    int[] _indices = Array.Empty<int>();
    ulong[] _keys = Array.Empty<ulong>();

    /// Number of visible items after Build
    public int Count { get; private set; }

    /// i is the position in the sorted visible list, not in the raw list
    public ref readonly RenderInfo this[int i] => ref CollectionsMarshal.AsSpan(_items)[_indices[i]];

    /// Raw unsorted, unculled items — for debug passes like wireframe
    public IReadOnlyList<RenderInfo> Items => _items;

    public void Add (RenderInfo info) {
        _items.Add(info);
    }

    public void Clear () {
        _items.Clear();
        Count = 0;
    }

    /// Cull + build keys + sort
    public void Build (in Frustum frustum, Vector3 camPos) {
        int total = _items.Count;
        if (_indices.Length < total) {
            _indices = new int[total];
            _keys = new ulong[total];
        }

        Span<RenderInfo> items = CollectionsMarshal.AsSpan(_items);
        Count = 0;
        for (int i = 0; i < total; i++) {
            ref readonly RenderInfo info = ref items[i];
            if (info.mesh is null || info.material is null) continue;

            if (info.material.Pass != RenderPass.UI) {
                AABB worldAABB = info.mesh.LocalAABB.Transformed(info.model);
                if (!frustum.Intersects(worldAABB)) continue;
            } /// UI is screen-space, so the world-space frustum test doesn't apply

            _indices[Count] = i;
            _keys[Count] = MakeSortKey(info, camPos);
            Count++;
        }

        /// Sorts the keys and moves the indices along with them
        bool sorted = true;
        for (int i = 1; i < Count; i++) {
            if (_keys[i] < _keys[i - 1]) { sorted = false; break; }
        }
        if (!sorted) Array.Sort(_keys, _indices, 0, Count);
    }

    /// End (exclusive) of the run of identical mesh+material that starts at start
    public int RunEnd (int start) {
        ref readonly RenderInfo first = ref this[start];
        int end = start + 1;
        while (end < Count) {
            ref readonly RenderInfo next = ref this[end];
            if (!ReferenceEquals(next.mesh, first.mesh) || !ReferenceEquals(next.material, first.material)) break;
            end++;
        }
        return end;
    }

    /// Opaque/UI: pass 2 bits | shader 22 bits | material 20 bits | mesh 20 bits
    /// Transparent: pass 2 bits | inverted distance 32 bits (back-to-front, required for correct blending)
    /// Relies on RenderPass order Opaque < Transparent < UI and on Id fields in Shader/Material/Mesh
    static ulong MakeSortKey (in RenderInfo info, Vector3 camPos) {
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
