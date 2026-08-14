using Newtonsoft.Json;

namespace Engine;


public abstract class ColliderComponent : Component, IComponentUpdate {

    [JsonIgnore] public bool drawGizmos = true;
    //public bool isStatic = false;


    public abstract void Update ();
    //public abstract void FixedUpdate ();


}
