using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics.UI;


public class UIRaycaster {
    List<UIElement> _elements = new();

    /// Finds topmost interactable element under the mouse point, or null
    public UIElement Raycast (float mouseX, float mouseY) {
        UIElement best = null;
        int bestZ = int.MinValue;

        foreach (var el in _elements) {
            if (!el.Interactable) continue;
            var rect = el.GetScreenRect();
            if (!rect.Contains(mouseX, mouseY)) continue;
            if (bestZ <= el.ZOrder) {
                bestZ = el.ZOrder;
                best = el;
            }
        }

        return best;
    }
}
