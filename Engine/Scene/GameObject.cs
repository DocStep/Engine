using System;
using System.Numerics;
using System.Collections.Generic;
using Engine.Graphics;

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


public class GameObject {
    public GameObject() {
        Id = GetHashCode();
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
        Transform.Rotation = rotation;
        Transform.Scale = scale;
        
        MeshComponent mesh = AddComponent<MeshComponent>();
        switch (primitive) {
            case PrimitiveTypes.Cube:
                mesh.mesh = Renderer.Instance._mesh_Cube;
                AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Sphere:
                mesh.mesh = Renderer.Instance._mesh_Sphere;
                AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Capsule:
                mesh.mesh = Renderer.Instance._mesh_Capsule;
                AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Plane:
                mesh.mesh = Renderer.Instance._mesh_Plane;
                AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.GizmoCube:
                MeshComponent colliderMesh = AddComponent<MeshComponent>();
                colliderMesh.mesh = Renderer.Instance._mesh_CubeWireframe;
                colliderMesh.material = Renderer.Instance._mat_GizmosG;
                colliderMesh.shader = Renderer.Instance._sh_Unlit;
                break;
            case PrimitiveTypes.GizmoSphere:
                colliderMesh = AddComponent<MeshComponent>();
                colliderMesh.mesh = Renderer.Instance._mesh_SphereWireframe;
                colliderMesh.material = Renderer.Instance._mat_GizmosG;
                colliderMesh.shader = Renderer.Instance._sh_Unlit;
                break;
            case PrimitiveTypes.GizmoCapsule:
                colliderMesh = AddComponent<MeshComponent>();
                colliderMesh.mesh = Renderer.Instance._mesh_CapsuleWireframe;
                colliderMesh.material = Renderer.Instance._mat_GizmosG;
                colliderMesh.shader = Renderer.Instance._sh_Unlit;
                break;
            case PrimitiveTypes.GizmoPlane:
                colliderMesh = AddComponent<MeshComponent>();
                colliderMesh.mesh = Renderer.Instance._mesh_PlaneWireframe;
                colliderMesh.material = Renderer.Instance._mat_GizmosG;
                colliderMesh.shader = Renderer.Instance._sh_Unlit;
                break;
            default:
                break;
        }
        SceneManager.ActiveScene.ObjectAdd(this);
    }



    public string Name = GameObject.GameObjectName;
    public readonly int Id = 0;
    public readonly TransformComponent Transform = new TransformComponent();
    private readonly List<Component> Components = new List<Component>();

    public const string GameObjectName = "GameObject";


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


}
