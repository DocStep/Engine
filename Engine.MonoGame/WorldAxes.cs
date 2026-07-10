using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGame;


public sealed class WorldAxes : IDisposable {
    private readonly VertexBuffer _vb;
    private readonly BasicEffect _effect;

    public WorldAxes (GraphicsDevice gd, float length = 2f) {
        _effect = new BasicEffect(gd) {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        VertexPositionColor[] vertices = new VertexPositionColor[] {
            // X axis (red)
            new(Vector3.Zero, Color.Red),
            new(new Vector3(length, 0, 0), Color.Red),

            // Y axis (green)
            new(Vector3.Zero, Color.Green),
            new(new Vector3(0, length, 0), Color.Green),

            // Z axis (blue)
            new(Vector3.Zero, Color.Blue),
            new(new Vector3(0, 0, length), Color.Blue),
        };

        _vb = new VertexBuffer(gd, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
        _vb.SetData(vertices);
    }

    public void Draw (GraphicsDevice gd, Matrix view, Matrix projection) {
        _effect.View = view;
        _effect.Projection = projection;
        _effect.World = Matrix.Identity;

        gd.SetVertexBuffer(_vb);

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes) {
            pass.Apply();

            gd.DrawPrimitives(
                PrimitiveType.LineList,
                0,
                3
            );
        }
    }

    public void Dispose () {
        _vb.Dispose();
        _effect.Dispose();
    }
}