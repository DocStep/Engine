using Newtonsoft.Json;

namespace Engine.Graphics.UI;


public class UIRenderingElement : Component {

    public override string Name => nameof(UIRenderingElement);

    [Hide][JsonIgnore] protected RectTransform rect = null!;
    [Hide][JsonIgnore] public RectTransform Rect => rect;


    public override void OnRemove () {
        ChangeRectToTransform();
    }

    protected void ChangeTransformToRect () {
        rect = gameObject.Transform as RectTransform ?? gameObject.AddComponent<RectTransform>();
    }
    protected void ChangeRectToTransform () {
        for (int i = 0; i < gameObject.Components.Count; i++) {
            UIRenderingElement? uiComponent = gameObject.Components[i] as UIRenderingElement;
            if (uiComponent == this) continue;
            if (uiComponent is not null) return;
        }

        gameObject.AddComponent<Transform>();
        rect = null!;
    }

    public virtual void Submit () {

    }

}
