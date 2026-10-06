using Engine.Graphics;

namespace Engine.Graphics;

public enum TonemapMode {
    Clamp = 0,
    PBRNeutral = 1,
    ACES = 2,
    Reinhard = 3,
    ReinhardExt = 4,
    Hable = 5,
    HableMap = 6,
}


public class Camera : Component {
    public Camera () {
        //priority = 0 < Cameras.Count ? Cameras[0].priority : 0;
        //Cameras.Insert(0, this);
        //Log.log(GetType(), priority);
    }

    public readonly static List<Camera> Cameras = new List<Camera>();

    public static Camera? Main {
        get {
            Camera? best = null;
            for (int i = 0; i < Cameras.Count; i++) {
                if (best == null || best.priority <= Cameras[i].priority) best = Cameras[i];
            }
            return best;
        }
    }


    [Hide, Newtonsoft.Json.JsonIgnore] public virtual Vector3 CameraPos => gameObject.Transform.Position;
    //public Matrix4x4 cameraRot = Matrix4x4.Identity;
    //public Vector2 mousePos_Window = Vector2.Zero;
    //[Hide] public static bool wantWarpPos = false;

    [Hide, Newtonsoft.Json.JsonIgnore] 
    public float fov = 75;
    public float FOV {
        get => fov;
        set {
            if (fov == value) return;
            fov = Mathf.Clamp(value, 1, 179);
        }
    }
    [Hide, Newtonsoft.Json.JsonIgnore] 
    private const float minPlane = 0.0001f;
    [Hide, Newtonsoft.Json.JsonIgnore] 
    public float near = 0.1f;
    public float Near {
        get => near;
        set {
            if (near == value) return;
            near = Mathf.Clamp(value, minPlane, value);
        }
    }
    [Hide, Newtonsoft.Json.JsonIgnore] 
    public float far = 1000f;
    public float Far {
        get => far;
        set {
            if (far == value) return;
            far = Mathf.Clamp(value, MathF.Max(near, minPlane) + minPlane, value);
        }
    }
    public float Exposure = 1f;
    public TonemapMode TonemapMode = TonemapMode.PBRNeutral;
    [Hide, Newtonsoft.Json.JsonIgnore] 
    protected float priority = 0f;
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
    public override void OnRemove () {
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
