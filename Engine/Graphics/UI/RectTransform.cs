using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public class RectTransform : Component {

    public override string Name => nameof(RectTransform);

    public Vector2 Size { get; set; } = new Vector2(100, 100);
    public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);

    /// (0,0) = top-left, (1,1) = bottom-right
    public Vector2 Anchor { get; set; } = new Vector2(0.5f, 0.5f);
    public Vector2 AnchoredPosition { get; set; } = Vector2.Zero;

    public bool RaycastTarget { get; set; } = true;
    public RaycastLayer Layer { get; set; } = RaycastLayer.Default;
    public int RaycastPriority { get; set; } = 0;

    [Hide][JsonIgnore]
    public Vector2 WorldPosition {
        get {
            RectTransform? parent = FindParentRect();
            Vector2 anchorPoint = parent is not null
                ? parent.Min + (parent.Max-parent.Min)*Anchor
                : Vector2.Zero;
            return anchorPoint + AnchoredPosition;
        }
    }

    [Hide][JsonIgnore] public Vector2 Min => WorldPosition - Size*Pivot;
    [Hide][JsonIgnore] public Vector2 Max => Min + Size;


    public bool Contains (Vector2 point) {
        Vector2 min = Min;
        Vector2 max = Max;
        return min.X <= point.X && point.X <= max.X && min.Y <= point.Y && point.Y <= max.Y;
    }

    private RectTransform? FindParentRect () {
        Transform? p = gameObject.Transform.Parent;
        while (p is not null) {
            RectTransform? r = p.gameObject.GetComponent<RectTransform>();
            if (r is not null) return r;
            p = p.Parent;
        }
        return null;
    }

}