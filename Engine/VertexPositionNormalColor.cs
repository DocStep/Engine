using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine;

/// <summary>
/// Vertex with position + normal + color. The normal is what lets
/// BasicEffect (or a custom shader later) calculate lighting per-face.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionNormalColor : IVertexType {
    public Vector3 Position;
    public Vector3 Normal;
    public Color Color;

    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VertexPositionNormalColor (Vector3 position, Vector3 normal, Color color) {
        Position = position;
        Normal = normal;
        Color = color;
    }
}
