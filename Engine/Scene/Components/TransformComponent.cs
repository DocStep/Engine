using System;
using System.Numerics;

namespace Engine;


public class TransformComponent : Component {

    public Vector3 position = Vector3.Zero;
    public Vector3 rotation = Vector3.Zero;
    public Vector3 scale = Vector3.One;

}
