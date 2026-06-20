using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGame;


public sealed class WireframeCube : IDisposable {
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;
    private readonly BasicEffect _effect;
    private readonly int _indexCount;

    public WireframeCube (GraphicsDevice gd, float size = 50f) {
        _effect = new BasicEffect(gd) {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        float s = size;

        VertexPositionColor[] vertices = {
            new(new Vector3(-s, -s, -s), Color.Gray),
            new(new Vector3(s, -s, -s), Color.Gray),
            new(new Vector3(s, s, -s), Color.Gray),
            new(new Vector3(-s, s, -s), Color.Gray),

            new(new Vector3(-s, -s, s), Color.Gray),
            new(new Vector3(s, -s, s), Color.Gray),
            new(new Vector3(s, s, s), Color.Gray),
            new(new Vector3(-s, s, s), Color.Gray),
        };

        ushort[] indices = {
            0,1, 1,2, 2,3, 3,0, // back
            4,5, 5,6, 6,7, 7,4, // front
            0,4, 1,5, 2,6, 3,7  // connections
        };

        _vb = new VertexBuffer(gd, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
        _vb.SetData(vertices);

        _ib = new IndexBuffer(gd, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _ib.SetData(indices);

        _indexCount = indices.Length;
    }

    public void Draw (GraphicsDevice gd, Matrix view, Matrix projection, Vector3 cameraPos) {
        gd.SetVertexBuffer(_vb);
        gd.Indices = _ib;

        _effect.View = view;
        _effect.Projection = projection;

        // keep skybox centered on camera
        _effect.World = Matrix.CreateTranslation(cameraPos);

        foreach (var pass in _effect.CurrentTechnique.Passes) {
            pass.Apply();
            gd.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _indexCount / 2);
        }
    }

    public void Dispose () {
        _vb.Dispose();
        _ib.Dispose();
        _effect.Dispose();
    }
}