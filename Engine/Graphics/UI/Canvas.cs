using Newtonsoft.Json;

namespace Engine.Graphics.UI;


public class Canvas : Component, IUpdate {

    public override string Name => nameof(Canvas);

    [Hide][JsonIgnore] private RectTransform rect = null!;
    [Hide][JsonIgnore] public RectTransform Rect => rect;


    public override void OnAdd () {
        rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rect.Pivot = new Vector2(0f, 0f);
        rect.AnchorMin = new Vector2(0f, 0f);
        rect.AnchorMax = new Vector2(1f, 1f);
        rect.AnchoredPosition = Vector2.Zero;
        SyncScreenSize();
    }

    public void Update () {
        SyncScreenSize();
        CollectChildren(gameObject.Transform);
    }
    private void SyncScreenSize () {
        rect.Size = new Vector2(Renderer.Instance.Width, Renderer.Instance.Height);
    }
    private void CollectChildren (Transform tr) {
        if (!tr.Enabled) return;

        tr.gameObject.GetComponent<Image>()?.Submit();
        tr.gameObject.GetComponent<TextComponent>()?.Submit();


        foreach (Transform child in tr.Children)
            CollectChildren(child);
    }


    public GameObject? Pick (Vector2 mousePos, RaycastLayer mask = RaycastLayer.All) {
        List<(GameObject go, int priority, int order)> hits = new();
        int order = 0;
        CollectHits(gameObject.Transform, mousePos, mask, hits, ref order);

        if (hits.Count == 0) return null;

        /// highest priority wins; ties broken by draw order (topmost/last-drawn)
        hits.Sort((a, b) => {
            int cmp = b.priority.CompareTo(a.priority);
            return cmp != 0 ? cmp : b.order.CompareTo(a.order);
        });

        return hits[0].go;
    }

    private void CollectHits (Transform t, Vector2 mousePos, RaycastLayer mask, List<(GameObject, int, int)> hits, ref int order) {
        if (!t.Enabled) return;

        RectTransform? rect = t.gameObject.GetComponent<RectTransform>();
        if (rect is not null && rect.Enabled && rect.RaycastTarget && (rect.Layer & mask) != 0 && rect.Contains(mousePos))
            hits.Add((t.gameObject, rect.RaycastPriority, order++));

        foreach (Transform child in t.Children)
            CollectHits(child, mousePos, mask, hits, ref order);
    }

}