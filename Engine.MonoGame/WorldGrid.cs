using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGame;


public sealed class WorldGrid : IDisposable {
    private readonly VertexBuffer _vb;
    private readonly BasicEffect _effect;
    private readonly int _lineCount;

    public WorldGrid (GraphicsDevice gd, int halfSize = 100, float cellSize = 1f) {
        _effect = new BasicEffect(gd) {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        var verts = new List<VertexPositionColor>();

        Color minor = new Color(40, 40, 40);
        Color major = new Color(80, 80, 80);

        for (int i = -halfSize; i <= halfSize; i++) {
            bool isMajor = i % 10 == 0;

            Color c = isMajor ? major : minor;

            float p = i * cellSize;

            // lines parallel to X (Z constant)
            verts.Add(new(new Vector3(-halfSize, 0, p), c));
            verts.Add(new(new Vector3(halfSize, 0, p), c));

            // lines parallel to Z (X constant)
            verts.Add(new(new Vector3(p, 0, -halfSize), c));
            verts.Add(new(new Vector3(p, 0, halfSize), c));
        }

        _lineCount = verts.Count / 2;

        _vb = new VertexBuffer(
            gd,
            typeof(VertexPositionColor),
            verts.Count,
            BufferUsage.WriteOnly
        );

        _vb.SetData(verts.ToArray());
    }

    public void Draw (GraphicsDevice gd, Matrix view, Matrix projection) {
        _effect.World = Matrix.Identity;
        _effect.View = view;
        _effect.Projection = projection;

        gd.SetVertexBuffer(_vb);

        foreach (var pass in _effect.CurrentTechnique.Passes) {
            pass.Apply();

            gd.DrawPrimitives(
                PrimitiveType.LineList,
                0,
                _lineCount
            );
        }
    }

    public void Dispose () {
        _vb.Dispose();
        _effect.Dispose();
    }
}