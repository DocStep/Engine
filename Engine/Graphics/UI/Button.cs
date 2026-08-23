using Newtonsoft.Json;
using Silk.NET.OpenGL;

namespace Engine.Graphics.UI;

public class Button : Component, IUpdate {

    public override string Name => nameof(Button);

    [Hide][JsonIgnore] public Action? de_Clicked = null;

    public Vector4 TintNormal { get; set; } = new Vector4(1f, 1f, 1f, 1f);
    public Vector4 TintHover { get; set; } = new Vector4(0.85f, 0.85f, 0.85f, 1f);
    public Vector4 TintPressed { get; set; } = new Vector4(0.65f, 0.65f, 0.65f, 1f);

    [Hide][JsonIgnore] private bool _hovered = false;
    [Hide][JsonIgnore] private bool _pressed = false;
    [Hide][JsonIgnore] private Image? _image;


    public override void OnAdd () {
        _image = gameObject.GetComponent<Image>();
    }

    public void Update () {
        if (_image is null) return;

        Vector2 mouse = Input.Inputs.MousePos;
        Vector3 pos = gameObject.Transform.Position;

        bool posInside =
            pos.X <= mouse.X && mouse.X <= pos.X + _image.Size.X &&
            pos.Y <= mouse.Y && mouse.Y <= pos.Y + _image.Size.Y;

        _hovered = posInside;

        bool down = Input.Inputs.Actions[Input.Inputs.LMB].pressedDown;
        bool up = Input.Inputs.Actions[Input.Inputs.LMB].pressedUp;

        if (posInside && down) _pressed = true;

        if (_pressed && up) {
            _pressed = false;
            if (posInside) de_Clicked?.Invoke();
        }

        if (!posInside && up) _pressed = false;

        _image.Tint = _pressed ? TintPressed : _hovered ? TintHover : TintNormal;
    }

}