using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public enum GridStartCorner { TopLeft, TopRight, BottomLeft, BottomRight }
public enum GridStartAxis { Horizontal, Vertical } /// Horizontal = fill columns first (row-major), Vertical = fill rows first (column-major)
public enum GridConstraint { Flexible, FixedColumnCount, FixedRowCount }
public enum GridAlignment { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }


public class GridUI : UIRenderingElement, IUpdate {

    public override string Name => nameof(GridUI);

    public Vector2 CellSize { get; set; } = new Vector2(200f, 50f);
    public Vector2 Spacing { get; set; } = new Vector2(10f, 10f);
    public Vector2 PaddingMin { get; set; } = Vector2.Zero; /// left, top
    public Vector2 PaddingMax { get; set; } = Vector2.One; /// right, bottom

    public GridConstraint Constraint { get; set; } = GridConstraint.FixedColumnCount;
    public int ConstraintCount { get; set; } = 1; /// columns if FixedColumnCount, rows if FixedRowCount — ignored if Flexible
    public GridStartCorner StartCorner { get; set; } = GridStartCorner.TopLeft;
    public GridStartAxis StartAxis { get; set; } = GridStartAxis.Vertical;
    public GridAlignment ChildAlignment { get; set; } = GridAlignment.UpperLeft; /// where the grid block sits if it's smaller than the available area


    public override void OnAdd () {
        rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
    }

    public void Update () {
        Layout();
    }

    public void Layout () {
        List<RectTransform> children = new();
        foreach (Transform child in gameObject.Transform.Children) {
            if (!child.Enabled) continue;
            RectTransform? childRect = child.gameObject.GetComponent<RectTransform>();
            if (childRect is not null) children.Add(childRect);
        }
        if (children.Count == 0) return;

        Vector2 availableSize = rect.ActualSize - PaddingMin - PaddingMax;
        Vector2 cellStep = CellSize + Spacing;

        int columns, rows;
        switch (Constraint) {
            case GridConstraint.FixedColumnCount:
                columns = Math.Max(1, ConstraintCount);
                rows = (int)MathF.Ceiling(children.Count/(float)columns);
                break;
            case GridConstraint.FixedRowCount:
                rows = Math.Max(1, ConstraintCount);
                columns = (int)MathF.Ceiling(children.Count/(float)rows);
                break;
            default:
                columns = Math.Max(1, (int)MathF.Floor((availableSize.X + Spacing.X)/cellStep.X));
                rows = (int)MathF.Ceiling(children.Count/(float)columns);
                break;
        }

        bool rowMajor = StartAxis == GridStartAxis.Horizontal;
        int primaryCount = rowMajor ? columns : rows;

        bool fromRight = StartCorner is GridStartCorner.TopRight or GridStartCorner.BottomRight;
        bool fromBottom = StartCorner is GridStartCorner.BottomLeft or GridStartCorner.BottomRight;

        Vector2 gridSize = new Vector2(columns*cellStep.X - Spacing.X, rows*cellStep.Y - Spacing.Y);
        Vector2 slack = availableSize - gridSize;

        float alignX = ChildAlignment switch {
            GridAlignment.UpperCenter or GridAlignment.MiddleCenter or GridAlignment.LowerCenter => 0.5f,
            GridAlignment.UpperRight or GridAlignment.MiddleRight or GridAlignment.LowerRight => 1f,
            _ => 0f,
        };
        float alignY = ChildAlignment switch {
            GridAlignment.MiddleLeft or GridAlignment.MiddleCenter or GridAlignment.MiddleRight => 0.5f,
            GridAlignment.LowerLeft or GridAlignment.LowerCenter or GridAlignment.LowerRight => 1f,
            _ => 0f,
        };
        Vector2 origin = PaddingMin + slack*new Vector2(alignX, alignY);

        for (int i = 0; i < children.Count; i++) {
            int primary = i%primaryCount;
            int secondary = i/primaryCount;

            int col = rowMajor ? primary : secondary;
            int row = rowMajor ? secondary : primary;

            if (fromRight) col = columns - 1 - col;
            if (fromBottom) row = rows - 1 - row;

            RectTransform childRect = children[i];
            childRect.AnchorMin = new Vector2(0f, 0f);
            childRect.AnchorMax = new Vector2(0f, 0f);
            childRect.Pivot = new Vector2(0f, 0f);
            childRect.Size = CellSize;
            childRect.AnchoredPosition = origin + new Vector2(col*cellStep.X, row*cellStep.Y);
        }
    }

}