using System;
using System.Numerics;

namespace Engine;


public abstract class ColliderComponent : Component, IComponentUpdate {

    public bool drawGizmos = true;
    //public bool isStatic = false;


    public abstract void Update ();
    //public abstract void FixedUpdate ();

}
