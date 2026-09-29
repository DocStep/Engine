namespace Engine.Graphics;


public static class MtlToMaterial {

    public static Material[] Build (Dictionary<string, MtlData> mtl, Mesh mesh, Material template) {
        Material[] result = new Material[mesh.SubMeshes.Length];

        for (int i = 0; i < mesh.SubMeshes.Length; i++) {
            string name = mesh.SubMeshes[i].MaterialName;

            if (mtl.TryGetValue(name, out MtlData? data)) {
                Material mat = new Material(template);
                mat.Name = name;
                mat.vectors3[Shader.Color] = data.Kd; /// <- match your shader's actual uniform name
                if (1 <= data.d) mat.Pass = RenderPass.Opaque;
                result[i] = mat;
            } else {
                result[i] = template;
            }
        }

        return result;
    }

    public static Dictionary<string, Material> Build_Dict (Dictionary<string, MtlData> mtl, Material template) {
        Dictionary<string, Material> result = new Dictionary<string, Material>();

        foreach (var (name, data) in mtl) {
            Material material = new Material(template); /// clones shader + existing uniform dicts
            material.Name = name;
            material.vectors3[Shader.Color] = data.Kd; /// <- rename "Color" to whatever your shader actually reads
            result[name] = material;
        }

        return result;
    }

    public static Material[] LoadMaterialsFor (Mesh mesh, Material template) {
        string? mtlPath = Path.ChangeExtension(mesh.Path, ".mtl");
        if (!File.Exists(mtlPath)) return Array.Empty<Material>();

        Dictionary<string, MtlData> mtl = MtlLoader.Load(mtlPath);
        return MtlToMaterial.Build(mtl, mesh, template);
    }

}
