using Newtonsoft.Json;
using Silk.NET.OpenGL;

namespace Engine.Graphics.UI;


public class Image : UIRenderingElement {

    public override string Name => nameof(Image);

    public Vector4 Tint { get; set; } = new Vector4(1f, 1f, 1f, 1f);
    [Hide] public float Alpha {
        set {
            Vector4 tint = Tint;
            tint.W = value;
            Tint = tint;
        }
    }

    /// Get returns the actual rendered size; set writes rect.Size (the delta — only equals actual size when both anchor axes are point-anchored)
    [Hide][JsonIgnore]
    public Vector2 Size {
        get => rect.ActualSize;
        set => rect.Size = value;
    }

    [Hide][JsonIgnore] private static Mesh? _sharedQuad = null;
    private Texture? _texture;
    [Hide][JsonIgnore] public Texture? Texture {
        get => _texture;
        set {
            if (value is null) return;
            _texture = value;
            Material.textureId = value.Handle;
        }
    }
    [JsonIgnore] public MaterialUI Material = null!;


    public override void OnAdd () {
        ChangeTransformToRect();

        _sharedQuad ??= new Mesh(Plane.GenerateQuadUI());
        Material = new MaterialUI(AssetsEngine._sh_UI);
    }
    public override void OnRemove () {
        _texture?.Dispose();
    }


    public override void Submit () {
        if (_sharedQuad is null) return;

        Vector2 origin = rect.Min; /// Min already accounts for pivot
        Vector2 size = rect.ActualSize;
        Material.SetVector4(Shader.Tint, Tint);
        _texture!.Bind();

        Renderer.Instance.AddRenderInfo(new RenderInfo {
            //name = "UIImage",
            model = Matrix4x4.CreateScale(size.X, size.Y, 1f)
              *Matrix4x4.CreateTranslation(new Vector3(origin.X, origin.Y, 0f))
              *rect.RectMatrix,
            mesh = _sharedQuad,
            material = Material,
        });
    }

}