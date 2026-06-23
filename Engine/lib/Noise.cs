//using System;
using Newtonsoft.Json;

namespace Engine;


[Serializable]
public class Noise {
    public enum AddMode {
        na,
        Mid,
        Positive,
        Negative,
    }
    public enum MultiplyMode {
        na,
        Mask,
        SeaAbove,
    }

    //[HideInInspector] public static float noiseMargin = 100000f;
    [JsonIgnore][NonSerialized] public FastNoiseLite noise = new FastNoiseLite();
    public Noise (float frequency = 1f, int octaves = 1, FastNoiseLite.NoiseType mode = FastNoiseLite.NoiseType.OpenSimplex2) {
        //noise = new FastNoiseLite(seed);
        this.frequency = frequency;
        this.octaves = octaves;

        noise = new FastNoiseLite();
        SetFrequency(frequency);
        noise.SetNoiseType(mode);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);  // For layered noise
        //noise.SetFractalOctaves(1);
    }
    public bool enable = true;
    public float frequency;
    public int octaves = 0;
    public FastNoiseLite.NoiseType mode = FastNoiseLite.NoiseType.OpenSimplex2;

    public Noise SetFrequency (float frequency) {
        this.frequency = frequency;
        noise.SetFrequency(frequency);
        return this;
    }
    public Noise SetOctaves (int octaves) {
        this.octaves = octaves;
        noise.SetFractalOctaves(octaves);
        return this;
    }

    /// <summary> [-1, 1] </summary>
    public virtual float Value (Vec2 pos2) {
        if (!enable) return 0;
        return noise.GetNoise(pos2.x, pos2.y);
    }

    /// <summary> [0, 1] </summary>
    public static float Normalize (float value) {
        float f = 0.5f*(value + 1);
        return f < 0 ? 0 : (1 < f ? 1 : f);
    }

    public static float NoiseAdd (float noise, float sup, AddMode mode = AddMode.na) {
        switch (mode) {
            case AddMode.na:
                return (noise + sup)/2;
            case AddMode.Mid:
                /// -160
                return noise - sup/2;
            case AddMode.Positive:
                //noise -= sup/2;
                return 0 < noise ? noise : 0;
            case AddMode.Negative:
                //noise -= sup/2;
                return noise < 0 ? noise : 0;
            default:
                return noise;
        }
    }

    public static float NoiseMultiply (float height, float noise, Noise.MultiplyMode mode, float edge = 0f) {
        switch (mode) {
            case Noise.MultiplyMode.na:
                return height*noise;
            case Noise.MultiplyMode.Mask:
                return 0.5f < noise ? height*noise : height;
            //case Noise.MultiplyMode.SeaAbove:
            //    return edge < height ? height*noise : height;
            default:
                return noise;
        }
    }

}
