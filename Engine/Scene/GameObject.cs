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
    public GameObject (Transform Transform, List<Component> Components) : base() {
        this.Transform = Transform;
        this.Components = Components;
    }

    public string Name = GameObject.GameObjectName;
    public readonly Transform Transform = new Transform();
    private readonly List<Component> Components = new List<Component>();
    public void AddComponent (Component component) {
        component.owner = Transform;
        Components.Add(component);
    }
    public void RemoveComponent (Component component) {
        component.owner = null!;
        Components.Remove(component);
    }

    public const string GameObjectName = "GameObject";

    public void FixedUpdate () {

    }
    public void Update () {
        int count = Components.Count;
        for (int i = 0; i < count; i++) {
            Components[i].Update();
        }
    }


}
