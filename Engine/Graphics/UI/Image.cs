using Silk.NET.OpenGL;
using Newtonsoft.Json;

namespace Engine.Graphics.UI;


public class Image : Component {

    public override string Name => nameof(Image);

    [Hide] public string Path { get; set; } = "";
    [ChangeStep(1)] public Vector2 Size { get; set; } = new Vector2(100, 100);
    public Vector4 Tint { get; set; } = new Vector4(1f, 1f, 1f, 1f);

    [Hide][JsonIgnore] private static Mesh? _sharedQuad = null;
    [JsonIgnore] private MaterialUI _material = null!;
    [Hide][JsonIgnore] public uint _textureId;
    [Hide][JsonIgnore] private bool _loaded = false;


    public override void OnAdd () {
        _sharedQuad ??= new Mesh(Plane.GenerateQuadUI());
        _material = new MaterialUI(AssetsEngine._sh_UI);
        if (Path.Length > 0) Load(Path);
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

    internal void Submit () {
        if (!_loaded || _sharedQuad is null) return;

        Vector3 pos = gameObject.Transform.Position;
        _material.SetVector4("uTint", Tint);

        Renderer.Instance.AddRenderInfo(new RenderInfo {
            name = "UIImage",
            model = Matrix4x4.CreateScale(Size.X, Size.Y, 1f)*Matrix4x4.CreateTranslation(pos),
            mesh = _sharedQuad,
            material = _material,
        });
    }

    public override void OnRemove () {
        if (_textureId != 0) Renderer.GL.DeleteTexture(_textureId);
        /// _sharedQuad is intentionally not disposed here — it's shared, owned by the class not the instance
    }

}