using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics.UI;


public class UIElement {
    public UIRect Rect;
    public int ZOrder;
    public bool Interactable = true;
    public UIElement Parent;
    public List<UIElement> Children = new();

    /// Converts local rect to absolute screen rect, accounting for parent offset
    public UIRect GetScreenRect () {
        float x = Rect.X;
        float y = Rect.Y;
        var p = Parent;
        while (p != null) {
            x += p.Rect.X;
            y += p.Rect.Y;
            p = p.Parent;
        }
        return new UIRect { X = x, Y = y, Width = Rect.Width, Height = Rect.Height };
    }
}
