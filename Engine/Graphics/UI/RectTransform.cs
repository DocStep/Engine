using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public class RectTransform : Transform {

    public override string Name => nameof(RectTransform);

    /// Size on a stretched axis (AnchorMin != AnchorMax) is a delta from the anchor-driven size; 0 = exact fill
    [ChangeStep(1f)] public Vector2 Size { get; set; } = new Vector2(0, 0);
    public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);

    /// (0,0) = top-left, (1,1) = bottom-right, relative to parent rect
    [ChangeStep(0.01f)] public Vector2 AnchorMin { get; set; } = new Vector2(0.5f, 0.5f);
    [ChangeStep(0.01f)] public Vector2 AnchorMax { get; set; } = new Vector2(0.5f, 0.5f);

    [ChangeStep(1f)] public Vector2 AnchoredPosition { get; set; } = Vector2.Zero;

    public bool RaycastTarget { get; set; } = true;
    public RaycastLayer Layer { get; set; } = RaycastLayer.Default;
    public int RaycastPriority { get; set; } = 0;

    /// Pivot point in world/parent space (not the same as Min+Size*Pivot when stretched — use Min/Max for actual bounds)
    [Hide][JsonIgnore]
    public Vector2 WorldPosition => Min + ActualSize*Pivot;

    [Hide][JsonIgnore] public Vector2 Min => AnchorMinPos + AnchoredPosition - Size*Pivot;
    [Hide][JsonIgnore] public Vector2 Max => AnchorMaxPos + AnchoredPosition + Size*(Vector2.One-Pivot);

    /// Actual on-screen size — differs from Size on stretched axes; renderers should read this, not Size
    [Hide][JsonIgnore] public Vector2 ActualSize => Max-Min;

    [Hide][JsonIgnore]
    private Vector2 AnchorMinPos {
        get {
            RectTransform? parent = FindParentRect();
            return parent is not null ? parent.Min + (parent.Max-parent.Min)*AnchorMin : Vector2.Zero;
        }
    }

    [Hide][JsonIgnore]
    private Vector2 AnchorMaxPos {
        get {
            RectTransform? parent = FindParentRect();
            return parent is not null ? parent.Min + (parent.Max-parent.Min)*AnchorMax : Vector2.Zero;
        }
    }

    [Hide][JsonIgnore]
    public Matrix4x4 RectMatrix {
        get {
            Vector2 pivot = WorldPosition;
            return Matrix4x4.CreateTranslation(new Vector3(-pivot, 0f))
                 * Matrix4x4.CreateScale(new Vector3(LocalScale.X, LocalScale.Y, 1f))
                 * Matrix4x4.CreateRotationZ(LocalEuler.Z*Mathf.Deg2Rad)
                 * Matrix4x4.CreateTranslation(new Vector3(pivot, 0f));
        }
    }

    [Hide][JsonIgnore]
    public Matrix4x4 RectMatrixInverse {
        get {
            Matrix4x4.Invert(RectMatrix, out Matrix4x4 inv);
            return inv;
        }
    }


    public bool Contains (Vector2 point) {
        Vector2 local = Vector2.Transform(point, RectMatrixInverse);
        Vector2 min = Min;
        Vector2 max = Max;
        return min.X <= local.X && local.X <= max.X && min.Y <= local.Y && local.Y <= max.Y;
    }

    /// Apply a common anchor layout. Does not touch AnchoredPosition/Size — call ResetToAnchor() after for a clean fill/snap.
    public void SetAnchor (AnchorPreset preset) {
        (AnchorMin, AnchorMax) = preset switch {
            AnchorPreset.TopLeft => (new Vector2(0, 0), new Vector2(0, 0)),
            AnchorPreset.TopCenter => (new Vector2(0.5f, 0), new Vector2(0.5f, 0)),
            AnchorPreset.TopRight => (new Vector2(1, 0), new Vector2(1, 0)),
            AnchorPreset.MiddleLeft => (new Vector2(0, 0.5f), new Vector2(0, 0.5f)),
            AnchorPreset.MiddleCenter => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
            AnchorPreset.MiddleRight => (new Vector2(1, 0.5f), new Vector2(1, 0.5f)),
            AnchorPreset.BottomLeft => (new Vector2(0, 1), new Vector2(0, 1)),
            AnchorPreset.BottomCenter => (new Vector2(0.5f, 1), new Vector2(0.5f, 1)),
            AnchorPreset.BottomRight => (new Vector2(1, 1), new Vector2(1, 1)),
            AnchorPreset.StretchTop => (new Vector2(0, 0), new Vector2(1, 0)),
            AnchorPreset.StretchMiddle => (new Vector2(0, 0.5f), new Vector2(1, 0.5f)),
            AnchorPreset.StretchBottom => (new Vector2(0, 1), new Vector2(1, 1)),
            AnchorPreset.StretchLeft => (new Vector2(0, 0), new Vector2(0, 1)),
            AnchorPreset.StretchCenter => (new Vector2(0.5f, 0), new Vector2(0.5f, 1)),
            AnchorPreset.StretchRight => (new Vector2(1, 0), new Vector2(1, 1)),
            AnchorPreset.StretchAll => (new Vector2(0, 0), new Vector2(1, 1)),
            _ => (AnchorMin, AnchorMax)
        };
    }

    /// Zeroes AnchoredPosition and Size — snaps a point anchor to (0,0), or makes a stretch anchor fill exactly
    public void ResetToAnchor () {
        AnchoredPosition = Vector2.Zero;
        Size = Vector2.Zero;
    }

    private RectTransform? FindParentRect () {
        Transform? p = gameObject.Transform.Parent;
        while (p is not null) {
            RectTransform? r = p as RectTransform ?? p.gameObject.GetComponent<RectTransform>();
            if (r is not null) return r;
            p = p.Parent;
        }
        return null;
    }

}

public enum AnchorPreset {
    TopLeft, TopCenter, TopRight,
    MiddleLeft, MiddleCenter, MiddleRight,
    BottomLeft, BottomCenter, BottomRight,
    StretchTop, StretchMiddle, StretchBottom,
    StretchLeft, StretchCenter, StretchRight,
    StretchAll
}