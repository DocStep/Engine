namespace Engine.Graphics;


public class MtlData {
    public string Name = null!;
    public Vector3 Kd = Vector3.One; /// Diffuse Color
    public Vector3 Ka = Vector3.One; /// Ambient
    public Vector3 Ks = Vector3.Zero; /// Specular
    public float Ns = 0f; /// Specular Exponent
    public float d = 1f; /// Opacity
}


public static class MtlLoader {
    public static Dictionary<string, MtlData> Load (string path) {
        Dictionary<string, MtlData> result = new Dictionary<string, MtlData>();
        MtlData? current = null;

        foreach (string rawLine in File.ReadLines(path)) {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) continue;

            switch (tokens[0]) {
                case "newmtl":
                    current = new MtlData { Name = tokens[1] };
                    result[tokens[1]] = current;
                    break;
                case "Kd": /// Diffuse Color
                    if (current is not null) current.Kd = ParseVec3(tokens);
                    break;
                case "Ka": /// Ambient
                    if (current is not null) current.Ka = ParseVec3(tokens);
                    break;
                case "Ks": /// Specular
                    if (current is not null) current.Ks = ParseVec3(tokens);
                    break;
                case "Ns": /// Specular Exponent
                    if (current is not null) current.Ns = ParseFloat(tokens[1]);
                    break;
                case "d": /// Opacity
                    if (current is not null) current.d = ParseFloat(tokens[1]);
                    break;
            }
        }

        return result;
    }

    private static Vector3 ParseVec3 (string[] tokens) {
        return new Vector3(ParseFloat(tokens[1]), ParseFloat(tokens[2]), ParseFloat(tokens[3]));
    }

    private static float ParseFloat (string s) {
        return float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    }
}
