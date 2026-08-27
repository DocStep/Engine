using Newtonsoft.Json;

namespace Engine.Graphics.UI;


public class Button : Component, IUpdate {

    public override string Name => nameof(Button);

    [Hide][JsonIgnore] public Action? de_Clicked = null;

    [DrawColor] public Vector4 TintNormal { get; set; } = new Vector4(1f, 1f, 1f, 1f);
    [DrawColor] public Vector4 TintHover { get; set; } = new Vector4(0.85f, 0.85f, 0.85f, 1f);
    [DrawColor] public Vector4 TintPressed { get; set; } = new Vector4(0.65f, 0.65f, 0.65f, 1f);

    [Hide][JsonIgnore] private bool _hovered = false;
    [Hide][JsonIgnore] private bool _pressed = false;
    [Hide][JsonIgnore] private RectTransform _rect = null!;
    [Hide][JsonIgnore] private Image? _image;


    public override void OnAdd () {
        _rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        _image = gameObject.GetComponent<Image>();
    }

    public void Update () {
        Vector2 mouse = Input.Inputs.MousePos;
        _hovered = _rect.RaycastTarget && _rect.Contains(mouse);

        bool down = Input.Inputs.Actions[Input.Inputs.LMB].pressedDown;
        bool up = Input.Inputs.Actions[Input.Inputs.LMB].pressedUp;

        if (_hovered && down) _pressed = true;

        if (_pressed && up) {
            _pressed = false;
            if (_hovered) de_Clicked?.Invoke();
        }

        if (!_hovered && up) _pressed = false;

        if (_image is not null)
            _image.Tint = _pressed ? TintPressed : _hovered ? TintHover : TintNormal;
    }

}
