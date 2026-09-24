using Engine.Graphics;

//using Engine.Graphics;

namespace Engine.Graphics;


public class Camera : Component {
    public Camera () {
        //priority = 0 < Cameras.Count ? Cameras[0].priority : 0;
        //Cameras.Insert(0, this);
        //Log.log(GetType(), priority);
    }

    public override string Name { get; } = nameof(Camera);

    public readonly static List<Camera> Cameras = new List<Camera>();

    public static Camera? Main {
        get {
            Camera? best = null;
            for (int i = 0; i < Cameras.Count; i++) {
                if (best == null || best.Priority <= Cameras[i].Priority) best = Cameras[i];
            }
            return best;
        }
    }


    public virtual Vector3 CameraPos => gameObject.Transform.Position;
    //public Matrix4x4 cameraRot = Matrix4x4.Identity;
    //public Vector2 mousePos_Window = Vector2.Zero;
    public static bool wantWarpPos = false;

    public float FOV = 60;
    public float PlaneNear = 0.1f;
    public float PlaneFar = 1000f;
    public float Exposure = 1f;
    [Newtonsoft.Json.JsonIgnore] protected float priority = 0f;
    public float Priority {
        get => priority;
        set {
            if (priority == value) return;

            priority = value;
            Cameras.Remove(this);
            int i = 0;
            while (i < Cameras.Count && Cameras[i].priority < priority) i++;
            Cameras.Insert(i, this);

            //Log.log("Cameras");
            //for (int i = 0; i < count; i++) {
            //    Log.log(Cameras[i].Name, Cameras[i]._priority);
            //}
        }
    }


    public override void OnAdd () {
        Cameras.Add(this);
    }
    override public void OnRemove () {
        Cameras.Remove(this);
    }


    public virtual Matrix4x4 GetRotationMatrix () {
        return Matrix4x4.Identity;
    }
    public virtual Matrix4x4 GetViewMatrix () {
        Vector3 pos = gameObject.Transform.Position;
        return Matrix4x4.CreateLookAtLeftHanded(pos, pos + gameObject.Transform.Forward, gameObject.Transform.Up);
    }

    public virtual bool GetRayMouse (out Ray ray) {
        Vector2 sceneSize = Input.Inputs.MousePos_Window;
        Vector2 mousePos = Input.Inputs.MousePos_Window;
        ray = Raycast.ScreenPointToRay(mousePos.X, mousePos.Y, (int)sceneSize.X, (int)sceneSize.Y,
            Renderer.Instance.m4x4_View, Renderer.Instance.m4x4_Projection);
        return true;
    }

}
