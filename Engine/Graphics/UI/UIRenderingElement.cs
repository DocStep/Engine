using Newtonsoft.Json;

namespace Engine.Graphics.UI;


public class UIRenderingElement : Component {

    public override string Name => nameof(UIRenderingElement);

    [Hide][JsonIgnore] protected RectTransform rect = null!;
    [Hide][JsonIgnore] public RectTransform Rect => rect;


    protected void ChangeTransformToRect () {
        rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
    }


    public virtual void Submit () {

    }

}
