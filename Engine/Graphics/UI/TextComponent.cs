using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public enum TextAlignH { Left, Center, Right }
public enum TextAlignV { Top, Center, Bottom }


public class TextComponent : Component {

    public override string Name => nameof(TextComponent);

    public string Text { get; set; } = "";
    public int FontSize { get; set; } = 24;
    public Vector4 Color { get; set; } = new Vector4(0f, 0f, 0f, 1f);
    public TextAlignH AlignH { get; set; } = TextAlignH.Left;
    public TextAlignV AlignV { get; set; } = TextAlignV.Top;
    private MaterialUI _material = null!;

    [Hide][JsonIgnore] private RectTransform rect = null!;
    [Hide][JsonIgnore] public RectTransform Rect => rect;

    [Hide][JsonIgnore] public bool AutoSize { get; set; } = false;
    [Hide][JsonIgnore] public bool InvertY { get; set; } = false; /// swaps Top/Bottom alignment meaning
    [Hide][JsonIgnore] public bool YDown { get; set; } = true; /// glyph layout convention; keep true until a Y-up pass is needed

    [Hide][JsonIgnore] private FontAtlas _atlas = null!;
    [Hide][JsonIgnore] private Mesh? _mesh;
    [Hide][JsonIgnore] private string _lastValue = null!;
    [Hide][JsonIgnore] private int _lastFontSize = -1;
    [Hide][JsonIgnore] private float _textWidth;
    [Hide][JsonIgnore] private float _textHeight;


    public override void OnAdd () {
        rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rect.AnchorMin = new Vector2(0.5f, 0.5f);
        rect.AnchorMax = new Vector2(0.5f, .5f);
        LoadAtlas();
        _material = new MaterialUI(AssetsEngine._sh_UI);
        _material.textureId = _atlas.TextureId;
        Rebuild();
    }
    public override void OnRemove () {
        _mesh?.Dispose();
        _atlas.Dispose();
    }

    private void LoadAtlas () {
        _atlas?.Dispose();
        _atlas = FontAtlas.Load(AssetsEngine._fontData, FontSize);
        _lastFontSize = FontSize;
    }

    private void Rebuild () {
        bool fontChanged = FontSize != _lastFontSize;
        if (Text == _lastValue && !fontChanged) return;

        if (fontChanged) {
            LoadAtlas();
            _material.textureId = _atlas.TextureId;
        }
        _lastValue = Text;

        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();
        float cursorX = 0f;

        float minX = 0f, maxX = 0f;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (char c in Text) {
            if (!_atlas.Chars.TryGetValue(c, out FontAtlas.CharInfo ci)) continue;

            float x0, y0, x1, y1;
            if (YDown) {
                /// baseline at y=0, ascender extends upward (negative), descender downward (positive)
                x0 = cursorX + ci.XOffset;
                y0 = ci.YOffset;
                x1 = x0 + ci.Width;
                y1 = y0 + ci.Height;
            } else {
                /// mirrored for a future Y-up pass
                x0 = cursorX + ci.XOffset;
                y0 = -(ci.YOffset + ci.Height);
                x1 = x0 + ci.Width;
                y1 = y0 + ci.Height;
            }

            uint baseIndex = (uint)vertices.Count;

            vertices.Add(new Vertex { Position = new Vector3(x0, y0, 0f), UV = new Vector2(ci.U0, ci.V0) });
            vertices.Add(new Vertex { Position = new Vector3(x1, y0, 0f), UV = new Vector2(ci.U1, ci.V0) });
            vertices.Add(new Vertex { Position = new Vector3(x1, y1, 0f), UV = new Vector2(ci.U1, ci.V1) });
            vertices.Add(new Vertex { Position = new Vector3(x0, y1, 0f), UV = new Vector2(ci.U0, ci.V1) });

            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);

            if (x1 > maxX) maxX = x1;
            if (y0 < minY) minY = y0;
            if (y1 > maxY) maxY = y1;

            cursorX += ci.XAdvance;
        }

        _textWidth = maxX - minX;
        _textHeight = vertices.Count > 0 ? maxY - minY : 0f;

        /// shift vertices so the mesh's local box starts at (0,0) — matches _textWidth/_textHeight exactly
        if (vertices.Count > 0) {
            for (int i = 0; i < vertices.Count; i++) {
                Vertex v = vertices[i];
                v.Position.X -= minX;
                v.Position.Y -= minY;
                vertices[i] = v;
            }
        }

        _mesh?.Dispose();
        _mesh = vertices.Count > 0
            ? new Mesh(new MeshData(vertices.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Triangles))
            : null;

        if (AutoSize) rect.Size = new Vector2(_textWidth, _textHeight);
    }

    private Vector2 AlignOffset () {
        float ox = AlignH switch {
            TextAlignH.Center => (rect.Size.X - _textWidth)*0.5f,
            TextAlignH.Right => rect.Size.X - _textWidth,
            _ => 0f,
        };

        TextAlignV alignV = AlignV;
        if (InvertY) {
            alignV = alignV switch {
                TextAlignV.Top => TextAlignV.Bottom,
                TextAlignV.Bottom => TextAlignV.Top,
                _ => TextAlignV.Center,
            };
        }

        float oy = alignV switch {
            TextAlignV.Center => (rect.Size.Y - _textHeight)*0.5f,
            TextAlignV.Bottom => rect.Size.Y - _textHeight,
            _ => 0f,
        };
        return new Vector2(ox, oy);
    }

    public void Submit () {
        Rebuild();
        if (_mesh is null) return;

        Vector2 origin = rect.Min + AlignOffset();
        _material.SetVector4(Shader.Tint, Color);

        Renderer.Instance.AddRenderInfo(new RenderInfo {
            name = "UIText",
            model = Matrix4x4.CreateTranslation(new Vector3(origin.X, origin.Y, 0f)),
            mesh = _mesh,
            material = _material,
        });
    }

}