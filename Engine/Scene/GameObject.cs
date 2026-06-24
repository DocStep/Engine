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
    public readonly List<Component> Components = new List<Component>();

    public const string GameObjectName = "GameObject";

    public void FixedUpdate () {

    }
    public void Update () {
        int count = Components.Count;
        for (int i = 0; i < count; i++) {
            //Components[i].Update();

            if (Components[i] is Graphics.MeshComponent meshComponent) {
                Graphics.RenderInfo renderInfo = new Graphics.RenderInfo() {
                    name = Name,
                    pos = Transform.position,
                    rot = Transform.rotation,
                    scale = Transform.scale,

                    mesh = meshComponent.mesh,
                    shader = meshComponent.shader,
                    material = meshComponent.material,
                    primitiveType = meshComponent.primitiveType,
                };
                Graphics.Renderer.Instance.RenderList.Add(renderInfo);
            }
        }
    }


}
