namespace Engine.Graphics;

/// Packed L2 spherical harmonics irradiance probe, matches SampleIrradianceSH() layout in shader
public struct SHAmbientProbe {
    public Vector4 SHAr;
    public Vector4 SHAg;
    public Vector4 SHAb;
    public Vector4 SHBr;
    public Vector4 SHBg;
    public Vector4 SHBb;
    public Vector4 SHC;
    public float Intensity;
}
