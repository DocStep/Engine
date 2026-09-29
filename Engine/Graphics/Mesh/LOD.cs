namespace Engine.Graphics;


public static class LOD {
    static LOD () {
        //Distances = [15f, 50f, 100f];
        Distances = [15f, 50f];
    }

    private static Dictionary<Mesh, Mesh[]> tieredMesh = new Dictionary<Mesh, Mesh[]>();

    private static float[] distancesSquared;
    public static float[] Distances {
        get => distancesSquared;
        set {
            Array.Sort(value);
            lodCount = value.Length;
            distancesSquared = new float[lodCount];
            for (int i = 0; i < lodCount; i++) {
                distancesSquared[i] = value[i]*value[i];
            }
        }
    }
    public static int lodCount { get; private set; }


    public static Mesh GetLOD (Mesh mesh, float distSquared) {
        if (!Constants.useLOD) return mesh;

        if (!tieredMesh.TryGetValue(mesh, out Mesh[]? lods)) {
            lods = BuildLOD(mesh, lodCount);
        }

        int n = Math.Min(lodCount, lods.Length);
        for (int i = n - 1; i >= 0; i--) {
            if (distancesSquared[i] <= distSquared) return lods[i];
        }
        return mesh;
    }

    public static void RegisterLOD (Mesh original, Mesh[] lods) {
        if (lods.Length != lodCount)
            Log.log($"[LOD] {lods.Length} meshes registered for {lodCount}-tier Distances — GetLOD will clamp to the shorter length");
        tieredMesh[original] = lods;
    }

    /// Generates lodCount simplified copies of `mesh` at runtime via vertex clustering and
    /// registers them for GetLOD. Cell size grows each tier so farther LODs are coarser.
    /// If a tier collapses to an empty mesh (cellSize too large for this mesh's scale), it
    /// reuses the previous tier instead of producing something unrenderable.
    /// This does real GPU uploads per tier (new Mesh(...) calls Upload) — call it once at
    /// load/streaming time, not per frame.
    public static Mesh[] BuildLOD (Mesh mesh, int lodCount, float baseCellFraction = 0.01f, float growth = 2.5f) {
        if (mesh.Data is null) {
            Log.log($"[LOD] {mesh.Name} has no Data to build LODs from");
            return [];
        }

        Vector3 size = mesh.LocalAABB.Max - mesh.LocalAABB.Min;
        float diagonal = size.Length();
        if (diagonal <= 0f) diagonal = 1f; /// degenerate/point mesh — fall back to a sane default

        Mesh[] lods = new Mesh[lodCount];
        float cellSize = diagonal*baseCellFraction;
        Mesh previous = mesh;

        for (int i = 0; i < lodCount; i++) {
            MeshData simplified = mesh.Data.Simplify(cellSize);

            if (simplified.Indices.Length == 0) {
                Log.log($"[LOD] {mesh.Name} tier {i} collapsed to nothing at cellSize {cellSize} — reusing previous tier");
                lods[i] = previous;
            } else {
                simplified.RecalculateNormals();
                Mesh lod = new Mesh(simplified) { Name = $"{mesh.Name}_LOD{i}" };
                lods[i] = lod;
                previous = lod;
            }

            cellSize *= growth;
        }

        RegisterLOD(mesh, lods);
        return lods;
    }

}
