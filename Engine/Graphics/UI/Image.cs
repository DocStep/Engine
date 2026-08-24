using Newtonsoft.Json;
using Silk.NET.OpenGL;

namespace Engine.Graphics.UI;


public class Image : Component {

    public override string Name => nameof(Image);

    [Hide] public string Path { get; set; } = "";
    public Vector4 Tint { get; set; } = new Vector4(1f, 1f, 1f, 1f);
    [Hide][JsonIgnore]
    public Vector2 Size {
        get => rect.Size;
        set => rect.Size = value;
    }

    [Hide][JsonIgnore] private RectTransform rect = null!;
    [Hide][JsonIgnore] public RectTransform Rect => rect;

    [Hide][JsonIgnore] private static Mesh? _sharedQuad = null;
    [JsonIgnore] private MaterialUI _material = null!;
    [Hide][JsonIgnore] public uint _textureId;
    [Hide][JsonIgnore] private bool _loaded = false;


    public override void OnAdd () {
        rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        _sharedQuad ??= new Mesh(Plane.GenerateQuadUI());
        _material = new MaterialUI(AssetsEngine._sh_UI);
        if (0 < Path.Length) Load(Path);
    }
    public override void OnRemove () {
        if (_textureId != 0) Renderer.GL.DeleteTexture(_textureId);
    }

    public void Load (string path) {
        Path = path;
        GL GL = Renderer.GL;

        StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromMemory(File.ReadAllBytes(path), StbImageSharp.ColorComponents.RedGreenBlueAlpha);

        if (_textureId == 0) _textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        unsafe {
            fixed (byte* ptr = image.Data) {
                GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        _material.textureId = _textureId;
        _loaded = true;
    }

    public void Submit () {
        if (!_loaded || _sharedQuad is null) return;

        Vector2 origin = rect.Min; /// Min already accounts for pivot
        _material.SetVector4("uTint", Tint);

        Renderer.Instance.AddRenderInfo(new RenderInfo {
            name = "UIImage",
            model = Matrix4x4.CreateScale(rect.Size.X, rect.Size.Y, 1f)*Matrix4x4.CreateTranslation(new Vector3(origin.X, origin.Y, 0f)),
            mesh = _sharedQuad,
            material = _material,
        });
    }

}
