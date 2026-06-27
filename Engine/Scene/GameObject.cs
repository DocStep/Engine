using System;
using System.Numerics;
using System.Collections.Generic;

namespace Engine;


public class GameObject {
    public GameObject() {
        Id = GetHashCode();
        SceneManager.ActiveScene.ObjectAdd(this);
    }
    public GameObject (List<Component> Components) : base() {
        this.Components = Components;
    }
    public GameObject (TransformComponent Transform, List<Component> Components) : base() {
        this.Transform = Transform;
        this.Components = Components;
    }

    public string Name = GameObject.GameObjectName;
    public readonly int Id = 0;
    public readonly TransformComponent Transform = new TransformComponent();
    private readonly List<Component> Components = new List<Component>();


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
        ComponentManager.ComponentRegister(component);
        return component;
    }

    public void RemoveComponent (Component component) {
        component.owner = null!;
        Components.Remove(component);
        ComponentManager.ComponentUnregister(component);
    }



    public const string GameObjectName = "GameObject";


}
