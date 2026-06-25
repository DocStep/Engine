using System.Numerics;

namespace Engine;


[System.Serializable]
public class NoiseHeight : Noise {
    public NoiseHeight (float frequency = 1f, float strength = 100f, int octaves = 1,
        FastNoiseLite.NoiseType mode = FastNoiseLite.NoiseType.OpenSimplex2) : base(frequency, octaves, mode) {
        this.strength = strength;
        //Debug.Log($"init {noise}");

    }
    public float strength;

    /// <summary> strength*[-1, 1] </summary>
    public virtual float ValueHeight (Vector2 vec2) {
        //return strength*base.Value(pos2);
        return strength*noise.GetNoise(frequency*vec2.X, frequency*vec2.Y);
    }
}
