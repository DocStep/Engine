namespace Engine.Graphics;


public static class MtlToMaterial {

    public static Material[] Build (Dictionary<string, MtlData> mtl, Mesh mesh, Material template) {
        Material[] result = new Material[mesh.SubMeshes.Length];

        for (int i = 0; i < mesh.SubMeshes.Length; i++) {
            string name = mesh.SubMeshes[i].MaterialName;

            if (mtl.TryGetValue(name, out MtlData? data)) {
                result[i] = SetValue(name, data, template);
            } else {
                result[i] = template;
            }
        }

        return result;
    }

    public static Dictionary<string, Material> Build_Dict (Dictionary<string, MtlData> mtl, Material template) {
        Dictionary<string, Material> result = new Dictionary<string, Material>();

        foreach ((string name, MtlData data) in mtl)
            result[name] = SetValue(name, data, template);

        return result;
    }

    private static Material SetValue (string name, MtlData data, Material template) {
        Material mat = new Material(template);
        //mat.textures.TryAdd(Shader.Texture, Texture.White); /// guarantee this regardless of whether template's FillDefaults ran
        mat.Name = name;
        mat.vectors3[Shader.uColor] = data.Kd;
        mat.floats[Shader.uAlpha] = data.d;

        float roughness = 0f <= data.Pr ? data.Pr : EstimateRoughnessFromNs(data.Ns);
        float metallic = 0f <= data.Pm ? data.Pm : (data.illum == 3 || data.illum == 6 ? 1f : 0f);

        mat.floats[Shader.uSmoothness] = 1f - roughness;
        mat.floats[Shader.uMetallic] = metallic;

        bool transparent = data.d < 1f;
        mat.Pass = transparent ? RenderPass.Transparent : RenderPass.Opaque;
        mat.DepthWrite = !transparent;

        return mat;
    }
    private static float EstimateRoughnessFromNs (float ns) {
        return 1f - MathF.Sqrt(MathF.Max(ns, 0f)/1000f);
    }

    public static Material[] LoadMaterialsFor (Mesh mesh, Material template) {
        string? mtlPath = Path.ChangeExtension(mesh.Path, ".mtl");
        if (!File.Exists(mtlPath)) return Array.Empty<Material>();

        Dictionary<string, MtlData> mtl = MtlLoader.Load(mtlPath);
        return MtlToMaterial.Build(mtl, mesh, template);
    }

}
