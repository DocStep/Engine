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


public class GameObject : IDisposable, IAsset<GameObject> {
    public GameObject () {
        Id = lib.Id;
        InitTransform();
        SceneManager.ActiveScene.GameObjectAdd(this);
    }
    /// Used only by the deserializer — sets up JsonIgnore'd runtime state
    /// without touching the scene or generating a throwaway Id
    [JsonConstructor]
    private GameObject (bool _deserializing) {
        InitTransform();
        SceneManager.ActiveScene.GameObjectAdd(this);
    }
    void InitTransform () {
        Transform tr = new Transform();
        TransformHandle = new TransformHandle(tr);
        SetTransform(tr);
    }
    public GameObject (PrimitiveTypes primitive, Vector3 position = new Vector3(), 
        Vector3 rotation = new Vector3(), Vector3 scale = default, string? name = default) {
        if (string.IsNullOrEmpty(name)) name = primitive.GetType().Name;
        if (scale.Equals(default)) scale = Vector3.One;

        Id = lib.Id;
        Transform tr = new Transform();
        TransformHandle = new TransformHandle(tr);
        SetTransform(tr);
        Transform.Position = position;
        Transform.Rotation = Mathf.EulerToQuaternion(rotation);
        Transform.LocalScale = scale;
        
        MeshComponent mesh = AddComponent<MeshComponent>();
        switch (primitive) {
            case PrimitiveTypes.Cube:
                mesh.Mesh = AssetsEngine._mesh_Cube;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Sphere:
                mesh.Mesh = AssetsEngine._mesh_Sphere;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Capsule:
                mesh.Mesh = AssetsEngine._mesh_Capsule;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Plane:
                mesh.Mesh = AssetsEngine._mesh_Plane;
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
        SceneManager.ActiveScene.GameObjectAdd(this);
    }

    [JsonIgnore] public readonly static string typeName = typeof(GameObject).Name;

    public string Name { get; set; } = TypeName;
    public long Id { get; set; }
    public string? Path { get; set; }

    public bool Enabled = true;
    [JsonIgnore, Hide] private TransformHandle TransformHandle = null!;
    public Transform Transform => TransformHandle.Current;

    public readonly List<Component> Components = new List<Component>();
    [JsonIgnore, Hide] private bool destroyed = false;

    [JsonIgnore] public const string TypeName = nameof(GameObject);


    public T? GetComponent<T> () where T : Component {
        if (Transform is T transform) return transform;

        foreach (Component component in Components) {
            if (component is T match) return match;
        }
        //throw new Exception($"Component of type {typeof(T)} not found in GameObject {Name}");
        return null;
    }
    public T AddComponent<T> () where T : Component, new() => AddComponentInternal(new T());
    public T AddComponentInternal<T> (T component) where T : Component {
        if (component is Transform transform) {
            SetTransform(transform);
            return component;
        }

        Components.Add(component);
        component.SetParent(this);
        ComponentsManager.Instance.ComponentRegister(component);
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
            ComponentsManager.Instance.ComponentUnregister(component);
    }
    public void RemoveComponent (Component component) {
        if (component is Transform) return;

        component.gameObject = null!;
        Components.Remove(component);
        ComponentsManager.Instance.ComponentUnregister(component);
    }


    internal void SetTransform (Transform transform) {
        Transform? previous = TransformHandle.Current;
        transform.SetParent(this);

        if (previous is not null)
            transform.CopyFrom(previous);

        TransformHandle.Rebind(transform);
    }


    [JsonIgnore, Hide] public bool Destroyed = false;
    [JsonIgnore, Hide] private static readonly System.Collections.Concurrent.ConcurrentQueue<GameObject> DestroyQueue =
        new System.Collections.Concurrent.ConcurrentQueue<GameObject>();

    /// <summary>Safe to call from any thread.</summary>
    private static void EnqueueDestroy (GameObject go) {
        DestroyQueue.Enqueue(go);
    }

    public void Destroy () {
        if (Destroyed) return;
        Destroyed = true;
        EnqueueDestroy(this);
    }
    public void DestroyImmediate () {
        Transform.Parent = null;
        destroyed = true;

        int count = Components.Count;
        for (int i = count - 1; i >= 0; i--) {
            ComponentsManager.Instance.ComponentUnregister(Components[i]);
        }
        Components.Clear();

        ComponentsManager.Instance.ComponentUnregister(Transform);
        SceneManager.ActiveScene.GameObjects.Remove(this);

        Dispose();
    }

    /// <summary>Main thread only. Call once per frame before Update().</summary>
    public static void Flush () {
        //Log.log();
        while (DestroyQueue.TryDequeue(out GameObject? go)) {
            go.DestroyImmediate();
        }
    }




    public void Save (string path) {
        Prefab.Save(this, path);
    }

    public static GameObject Load (string path) {
        return Prefab.Load(path);
    }



    public void Dispose () {

    }

}
