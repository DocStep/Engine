using Silk.NET.OpenAL;

namespace Engine.Audio;


public unsafe class AudioManager {
    public AudioManager () {
        Instance = this;
        AL = AL.GetApi(true);
        ALC = ALContext.GetApi(true);
        device = ALC.OpenDevice("");
        context = ALC.CreateContext(device, null);
        ALC.MakeContextCurrent(context);

        for (int i = 0; i < pool.Length; i++) {
            pool[i] = AL.GenSource();
            sources.Add(pool[i]);
        }

        Windows.Window.FocusChanged += AudioManager.Instance.SetFocused;
    }

    public static AudioManager Instance = null!;

    public readonly AL AL = null!;
    public readonly ALContext ALC = null!;

    public static bool MuteOnUnfocus = true;

    private Device* device;
    private Context* context;
    private uint[] pool = new uint[32];
    private int next;

    private readonly HashSet<uint> sources = new();
    private readonly List<uint> suspended = new();
    private float master = 1f;
    private bool focused = true;

    /// <summary>Global volume, 0 to 1.</summary>
    public float MasterVolume {
        get => master;
        set {
            master = value;
            if (focused) AL.SetListenerProperty(ListenerFloat.Gain, master);
        }
    }

    /// <summary>Sources that must pause on unfocus. AudioSource registers itself.</summary>
    public void Register (uint src) => sources.Add(src);
    public void Unregister (uint src) {
        sources.Remove(src);
        suspended.Remove(src);
    }

    /// <summary>Call once per frame with the camera transform.</summary>
    public void UpdateListener (Vector3 pos, Vector3 forward, Vector3 up) {
        AL.SetListenerProperty(ListenerVector3.Position, pos.X, pos.Y, -pos.Z);
        float* ori = stackalloc float[6] { forward.X, forward.Y, -forward.Z, up.X, up.Y, -up.Z };
        AL.SetListenerProperty(ListenerFloatArray.Orientation, ori);
    }


    /// <summary>Fire-and-forget one-shot. Steals the oldest source when the pool is full.</summary>
    public void PlayOneShot (AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f) {
        if (!focused) return;

        uint src = pool[next];
        next = (next + 1) % pool.Length;

        AL.SourceStop(src);
        AL.SetSourceProperty(src, SourceInteger.Buffer, (int)clip.Buffer);
        AL.SetSourceProperty(src, SourceVector3.Position, position.X, position.Y, -position.Z); // LH to OpenAL RH
        AL.SetSourceProperty(src, SourceFloat.Gain, volume);
        AL.SetSourceProperty(src, SourceFloat.Pitch, pitch);
        AL.SetSourceProperty(src, SourceBoolean.Looping, false);
        AL.SourcePlay(src);
    }


    /// <summary>Call from the window FocusChanged event. Mutes and pauses while unfocused.</summary>
    public void SetFocused (bool focused) {
        if (this.focused == focused) return;

        this.focused = focused;
        if (this.focused) {
            foreach (uint s in suspended) AL.SourcePlay(s);
            suspended.Clear();
            AL.SetListenerProperty(ListenerFloat.Gain, master);
        } else {
            if (!MuteOnUnfocus) return;

            AL.SetListenerProperty(ListenerFloat.Gain, 0f);
            foreach (uint s in sources) {
                AL.GetSourceProperty(s, GetSourceInteger.SourceState, out int state);
                if (state != (int)SourceState.Playing) continue;
                AL.SourcePause(s);
                suspended.Add(s);
            }
        }
    }

    public void Shutdown () {
        foreach (uint s in pool) AL.DeleteSource(s);
        sources.Clear();
        suspended.Clear();
        ALC.MakeContextCurrent(null);
        ALC.DestroyContext(context);
        ALC.CloseDevice(device);
    }

}