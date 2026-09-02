using Newtonsoft.Json;

namespace Engine.Graphics.UI;


public enum FlexDirection { Row, Column }
public enum Justify { Start, Center, End, SpaceBetween }
public enum Align { Start, Center, End, Stretch }


/// <summary>
/// Style/layout parameters for a single UIElement. Width/Height of -1 means auto.
/// </summary>
public class UIStyle {
    public FlexDirection Direction = FlexDirection.Row;
    public Justify Justify = Justify.Start;
    public Align Align = Align.Stretch;
    public float Grow = 0;
    public float Width = -1;
    public float Height = -1;
}


/// <summary>
/// Non-GameObject UI node. Computes its own rect and its children's rects via Layout().
/// </summary>
public class UIElement {
    public UIStyle Style = new UIStyle();
    public List<UIElement> Children = new();

    [Hide][JsonIgnore] public Vector2 ComputedPosition;
    [Hide][JsonIgnore] public Vector2 ComputedSize;

    public void AddChild (UIElement child) { Children.Add(child); }

    /// <summary>
    /// Lays out children along the main axis using flex-grow, then aligns each
    /// along the cross axis. position/size are in pixels, top-left origin.
    /// </summary>
    public void Layout (Vector2 position, Vector2 size) {
        ComputedPosition = position;
        ComputedSize = size;

        bool isRow = Style.Direction == FlexDirection.Row;
        float mainSize = isRow ? size.X : size.Y;
        float crossSize = isRow ? size.Y : size.X;

        float totalFixed = 0;
        float totalGrow = 0;
        foreach (var child in Children) {
            float childMain = isRow ? child.Style.Width : child.Style.Height;
            if (childMain >= 0) totalFixed += childMain;
            totalGrow += child.Style.Grow;
        }

        float remaining = mainSize - totalFixed;
        float growUnit = totalGrow > 0 ? remaining / totalGrow : 0;

        float cursor = 0;
        float spaceBetween = 0;
        if (totalGrow == 0) {
            if (Style.Justify == Justify.Center) cursor = remaining * 0.5f;
            else if (Style.Justify == Justify.End) cursor = remaining;
            else if (Style.Justify == Justify.SpaceBetween && Children.Count > 1)
                spaceBetween = remaining / (Children.Count - 1);
        }

        foreach (var child in Children) {
            float childMain = isRow ? child.Style.Width : child.Style.Height;
            if (childMain < 0) childMain = child.Style.Grow > 0 ? growUnit * child.Style.Grow : 0;

            float childCross = isRow ? child.Style.Height : child.Style.Width;
            if (childCross < 0) childCross = Style.Align == Align.Stretch ? crossSize : 0;

            float crossOffset = 0;
            if (Style.Align == Align.Center) crossOffset = (crossSize - childCross) * 0.5f;
            else if (Style.Align == Align.End) crossOffset = crossSize - childCross;

            Vector2 childPos = isRow
                ? position + new Vector2(cursor, crossOffset)
                : position + new Vector2(crossOffset, cursor);
            Vector2 childSize = isRow
                ? new Vector2(childMain, childCross)
                : new Vector2(childCross, childMain);

            child.Layout(childPos, childSize);

            cursor += childMain + spaceBetween;
        }
    }
}
