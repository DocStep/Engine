using System;
using System.Numerics;

namespace Engine.Graphics;


public abstract class ColliderComponent : Component, IComponentUpdate, IComponentFixedUpdate {
    public abstract void Update ();
    public abstract void FixedUpdate ();
}
