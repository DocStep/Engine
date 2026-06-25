using System;
using System.Numerics;
using System.Collections.Generic;

namespace Engine;


public class GameObject {
    public GameObject() {
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
    public readonly TransformComponent Transform = new TransformComponent();
    private readonly List<Component> Components = new List<Component>();
    public void AddComponent (Component component) {
        component.owner = Transform;
        Components.Add(component);
        ComponentManager.ComponentRegister(component);
    }
    public void RemoveComponent (Component component) {
        component.owner = null!;
        Components.Remove(component);
        ComponentManager.ComponentUnregister(component);
    }

    public const string GameObjectName = "GameObject";


}
