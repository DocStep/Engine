using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Engine;

public sealed class Sphere : IDisposable {
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly BasicEffect _effect;
    private readonly int _indexCount;

    public Sphere (GraphicsDevice graphicsDevice, float radius = 0.15f, int segments = 16, Color? color = null) {
        _effect = new BasicEffect(graphicsDevice) {
            VertexColorEnabled = true,
            LightingEnabled = true,
            TextureEnabled = false,
        };

        //_effect.EnableDefaultLighting();
        //_effect.Alpha = 0.4f;

        //_effect.LightingEnabled = true;
        //_effect.DirectionalLight0.Enabled = true;
        //_effect.DirectionalLight0.Direction = new Vector3(-1, -1, -1);
        //_effect.DirectionalLight0.DiffuseColor = Color.White.ToVector3();

        var (vertices, indices) = BuildMesh(radius, segments, color ?? Color.Yellow);
        _indexCount = indices.Length;

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

    //Vector3 ComputeNormal (Vector3 a, Vector3 b, Vector3 c) {
    //    return Vector3.Normalize(Vector3.Cross(b - a, c - a));
    //}
    private static void BuildSmoothNormals (VertexPositionNormalColor[] vertices, short[] indices) {
        for (int i = 0; i < vertices.Length; i++)
            vertices[i].Normal = Vector3.Zero;

        for (int i = 0; i < indices.Length; i += 3) {
            int i0 = indices[i];
            int i1 = indices[i + 1];
            int i2 = indices[i + 2];

            Vector3 a = vertices[i0].Position;
            Vector3 b = vertices[i1].Position;
            Vector3 c = vertices[i2].Position;

            Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));

            vertices[i0].Normal += normal;
            vertices[i1].Normal += normal;
            vertices[i2].Normal += normal;
        }

        for (int i = 0; i < vertices.Length; i++)
            vertices[i].Normal.Normalize();
    }

    public void Draw (GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection, Color color) {
        _effect.World = world;
        _effect.View = view;
        _effect.Projection = projection;
        _effect.Alpha = color.A / 255f;
        _effect.DiffuseColor = color.ToVector3();

        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes) {
            pass.Apply();

            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                baseVertex: 0,
                startIndex: 0,
                primitiveCount: _indexCount / 3);
        }
    }

    private static (VertexPositionNormalColor[] vertices, short[] indices) BuildMesh (float radius, int segments, Color color) {
        int rings = segments;
        int sectors = segments;

        var vertices = new List<VertexPositionNormalColor>();

        for (int r = 0; r <= rings; r++) {
            float v = (float)r / rings;
            float phi = v * MathHelper.Pi;

            float y = MathF.Cos(phi);
            float ringRadius = MathF.Sin(phi);

            for (int s = 0; s <= sectors; s++) {
                float u = (float)s / sectors;
                float theta = u * MathHelper.TwoPi;

                float x = ringRadius * MathF.Cos(theta);
                float z = ringRadius * MathF.Sin(theta);

                Vector3 normal = Vector3.Normalize(new Vector3(x, y, z));
                //Vector3 normal = Vector3.Zero;
                Vector3 pos = normal * radius;

                vertices.Add(new VertexPositionNormalColor(pos, normal, color));
            }
        }

        var indices = new List<short>();
        int rowStride = sectors + 1;

        for (int r = 0; r < rings; r++) {
            for (int s = 0; s < sectors; s++) {
                short a = (short)(r * rowStride + s);
                short b = (short)(a + rowStride);
                short c = (short)(a + 1);
                short d = (short)(b + 1);

                indices.Add(a); indices.Add(c); indices.Add(b);
                indices.Add(b); indices.Add(c); indices.Add(d);
            }
        }

        var vArr = vertices.ToArray();
        var iArr = indices.ToArray();

        //BuildSmoothNormals(vArr, iArr);

        return (vArr, iArr);
    }

    public void Dispose () {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _effect.Dispose();
    }
}