using Newtonsoft.Json;

namespace Engine.Audio;


/// <summary>Ears of the scene. Put it on the camera. Only one is active at a time.</summary>
public class AudioListener : Component, IUpdate {

    [Hide, JsonIgnore] public static AudioListener? Active;


    public override void OnAdd () {
        Active ??= this;
    }

    public override void OnRemove () {
        if (Active == this) Active = null;
    }

    public void Update () {
        if (Active != this || !Enabled) return;

        var t = gameObject.Transform;
        AudioManager.Instance.UpdateListener(t.Position, t.Forward, t.Up);
    }

}