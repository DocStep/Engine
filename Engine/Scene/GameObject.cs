using Engine.Graphics;
using Newtonsoft.Json;

namespace Engine;

public enum PrimitiveTypes {
    Cube,
    Sphere,
    Capsule,
    Plane,

    GizmoCube,
    GizmoSphere,
    GizmoCapsule,
    GizmoPlane,
}


public class GameObject : ISavable, IDisposable {
    public GameObject() {
        Id = lib.Id;
        Transform.owner = this;
        SceneManager.ActiveScene.ObjectAdd(this);
    }
    /*public GameObject (List<Component> Components) : base() {
        this.Components = Components;
    }*/
    /*public GameObject (TransformComponent Transform, List<Component> Components) : base() {
        this.Transform = Transform;
        this.Components = Components;
    }*/
    public GameObject (PrimitiveTypes primitive, Vector3 position = new Vector3(), 
        Vector3 rotation = new Vector3(), Vector3 scale = default, string? name = default) {
        if (string.IsNullOrEmpty(name)) name = primitive.GetType().Name;
        if (scale.Equals(default)) scale = Vector3.One;

        Id = GetHashCode();

        Transform.owner = this;
        Transform.Position = position;
        Transform.Rotation = Mathf.EulerToQuaternion(rotation);
        Transform.Scale = scale;
        
        MeshComponent mesh = AddComponent<MeshComponent>();
        switch (primitive) {
            case PrimitiveTypes.Cube:
                mesh.mesh = AssetsEngine._mesh_Cube;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Sphere:
                mesh.mesh = AssetsEngine._mesh_Sphere;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Capsule:
                mesh.mesh = AssetsEngine._mesh_Capsule;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Plane:
                mesh.mesh = AssetsEngine._mesh_Plane;
                //AddComponent<PhysicsComponent>();
                break;

            /// <> <!>
            case PrimitiveTypes.GizmoCube:
                //MeshComponent colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_CubeWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            case PrimitiveTypes.GizmoSphere:
                //colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_SphereWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            case PrimitiveTypes.GizmoCapsule:
                //colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_CapsuleWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            case PrimitiveTypes.GizmoPlane:
                //colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_PlaneWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            default:
                break;
        }
        //mesh.material = AssetsEngine._mat_Lit;
        SceneManager.ActiveScene.ObjectAdd(this);
    }

    [JsonIgnore] public readonly static string typeName = typeof(GameObject).Name;

    public string Name = GameObject.GameObjectName;
    public readonly long Id = 0;
    public bool Enabled = true;
    public readonly Transform Transform = new Transform();
    public readonly List<Component> Components = new List<Component>();

    [JsonIgnore] public const string GameObjectName = "GameObject";


    public T? GetComponent<T> () where T : Component {
        foreach (Component component in Components) {
            if (component is T match) return match;
        }
        //throw new Exception($"Component of type {typeof(T)} not found in GameObject {Name}");
        return null;
    }
    public T AddComponent<T> () where T : Component, new() {
        T component = new T();
        Components.Add(component);
        component.SetParent(this);
        ComponentManager.Instance.ComponentRegister(component);
        return component;
    }

    public void RemoveComponent<T> () where T : Component, new() {
        T? component = null;
        foreach (Component comp in Components) {
            if (comp is T match) {
                component = match;
                break;
            }
        }
        if (component is not null) 
            ComponentManager.Instance.ComponentUnregister(component);
    }
    public void RemoveComponent (Component component) {
        component.owner = null!;
        Components.Remove(component);
        ComponentManager.Instance.ComponentUnregister(component);
    }


    public void PreSave () {
        /// Own
        /// ...

        int count = Components.Count;
        for (int i = 0; i < count; i++) {
            Components[i].PreSave();
        }
    }

    public void PostLoad () {
        
    }


    public void Dispose () {
        
    }

}
