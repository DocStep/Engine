using System;
using System.Numerics;

namespace Engine;


public class TransformComponent : Component {

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

}
