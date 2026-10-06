using Newtonsoft.Json;
using Silk.NET.OpenAL;

namespace Engine.Audio;


/// <summary>Owns one OpenAL source. Spatial sounds need MONO clips, OpenAL does not pan stereo.</summary>
public class AudioSource : Component {

    public string ClipPath = "";
    public float Volume = 1f;
    public float Pitch = 1f;
    public bool Loop;
    public bool PlayOnAdd = true;
    public bool Spatial = true;
    public float MinDistance = 1f;
    public float MaxDistance = 50f;
    public float Rolloff = 1f;

    [Hide, JsonIgnore] public AudioClip? Clip;

    uint src;
    AL al = null!;

    [Hide, JsonIgnore]
    public bool IsPlaying {
        get {
            al.GetSourceProperty(src, GetSourceInteger.SourceState, out int s);
            return s == (int)SourceState.Playing;
        }
    }

    public override void OnAdd () {
        al = AudioManager.Instance.AL;
        src = al.GenSource();

        if (Clip is null && !string.IsNullOrEmpty(ClipPath)) 
            Clip = AudioClip.Load(ClipPath);
        Apply();
        if (PlayOnAdd) Play();
    }

    public override void OnRemove () {
        al.SourceStop(src);
        al.SetSourceProperty(src, SourceInteger.Buffer, 0);
        al.DeleteSource(src);
    }

    public void Play () {
        if (Clip == null) return;
        al.SourceStop(src);
        al.SetSourceProperty(src, SourceInteger.Buffer, (int)Clip.Buffer);
        Apply();
        al.SourcePlay(src);
    }

    public void Pause () => al.SourcePause(src);
    public void Stop () => al.SourceStop(src);

    public void Update () {
        if (!Enabled) return;
        Apply();
    }

    void Apply () {
        var p = gameObject.Transform.Position;
        al.SetSourceProperty(src, SourceBoolean.SourceRelative, !Spatial);
        al.SetSourceProperty(src, SourceVector3.Position, Spatial ? p.X : 0f, Spatial ? p.Y : 0f, Spatial ? -p.Z : 0f); // LH to OpenAL RH
        al.SetSourceProperty(src, SourceFloat.Gain, Enabled ? Volume : 0f);
        al.SetSourceProperty(src, SourceFloat.Pitch, Pitch);
        al.SetSourceProperty(src, SourceBoolean.Looping, Loop);
        al.SetSourceProperty(src, SourceFloat.ReferenceDistance, MinDistance);
        al.SetSourceProperty(src, SourceFloat.MaxDistance, MaxDistance);
        al.SetSourceProperty(src, SourceFloat.RolloffFactor, Rolloff);
    }

}
