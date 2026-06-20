using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGame;

/// <summary>
/// A basic colored cube mesh with per-face normals, so BasicEffect's lighting
/// can shade each face correctly. Swap the BasicEffect for a custom Effect
/// (HLSL) later for custom shaders.
/// </summary>
public sealed class Cube : IDisposable {
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly BasicEffect _effect;

    public BasicEffect Effect => _effect;

    public Cube (GraphicsDevice graphicsDevice) {
        _effect = new BasicEffect(graphicsDevice) {
            VertexColorEnabled = true,
            LightingEnabled = true,
            PreferPerPixelLighting = true,
        };

        // One directional light coming from upper-right-front, plus soft ambient
        // so faces aren't pure black in shadow.
        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.5f, -0.7f, -0.5f));
        _effect.DirectionalLight0.DiffuseColor = Vector3.One; // white light
        _effect.AmbientLightColor = new Vector3(0.25f, 0.25f, 0.25f);

        var (vertices, indices) = BuildMesh();

        _vertexBuffer = new VertexBuffer(
            graphicsDevice,
            VertexPositionNormalColor.VertexDeclaration,
            vertices.Length,
            BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _indexBuffer = new IndexBuffer(
            graphicsDevice,
            IndexElementSize.SixteenBits,
            indices.Length,
            BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
    }

    public void Draw (GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection) {
        _effect.World = world;
        _effect.View = view;
        _effect.Projection = projection;

        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes) {
            pass.Apply();
            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                baseVertex: 0,
                startIndex: 0,
                primitiveCount: _indexBuffer.IndexCount / 3);
        }
    }

    private static (VertexPositionNormalColor[] vertices, short[] indices) BuildMesh () {
        var faces = new[] {
            (normal: Vector3.Forward, right: Vector3.Right, up: Vector3.Up),       // +Z front
            (normal: Vector3.Backward, right: Vector3.Left, up: Vector3.Up),       // -Z back
            (normal: Vector3.Right, right: Vector3.Backward, up: Vector3.Up),      // +X right
            (normal: Vector3.Left, right: Vector3.Forward, up: Vector3.Up),        // -X left
            (normal: Vector3.Up, right: Vector3.Right, up: Vector3.Backward),      // +Y top
            (normal: Vector3.Down, right: Vector3.Right, up: Vector3.Forward),     // -Y bottom
        };

        var vertices = new VertexPositionNormalColor[24];
        var indices = new short[36];
        Color color = Color.LightGray;

        for (int f = 0; f < faces.Length; f++) {
            var (normal, right, up) = faces[f];
            Vector3 center = normal * 0.5f;

            Vector3 bl = center - right * 0.5f - up * 0.5f;
            Vector3 br = center + right * 0.5f - up * 0.5f;
            Vector3 tr = center + right * 0.5f + up * 0.5f;
            Vector3 tl = center - right * 0.5f + up * 0.5f;

            int vBase = f * 4;
            vertices[vBase + 0] = new VertexPositionNormalColor(bl, normal, color);
            vertices[vBase + 1] = new VertexPositionNormalColor(br, normal, color);
            vertices[vBase + 2] = new VertexPositionNormalColor(tr, normal, color);
            vertices[vBase + 3] = new VertexPositionNormalColor(tl, normal, color);

            int iBase = f * 6;
            indices[iBase + 0] = (short)(vBase + 0);
            indices[iBase + 1] = (short)(vBase + 2);
            indices[iBase + 2] = (short)(vBase + 1);
            indices[iBase + 3] = (short)(vBase + 0);
            indices[iBase + 4] = (short)(vBase + 3);
            indices[iBase + 5] = (short)(vBase + 2);
        }

        return (vertices, indices);
    }

    public void Dispose () {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _effect.Dispose();
    }
}