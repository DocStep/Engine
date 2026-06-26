using System;
using System.Numerics;

namespace Engine.Bad;


public abstract class ColliderComponent : Component, IComponentUpdate {

    public bool drawGizmos = true;
    public bool isStatic = false;

    public override void OnAdd () {
        PhysicsManager.Register(this);
    }
    public override void OnDestroy () {
        PhysicsManager.Unregister(this);
    }

    public abstract Bounds GetWorldBounds ();
    public abstract bool Overlaps (ColliderComponent other, out Contact1 contact);


    public abstract void Update ();
    //public abstract void FixedUpdate ();
}
