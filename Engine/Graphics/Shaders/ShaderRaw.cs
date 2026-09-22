
namespace Engine.Graphics;


public class ShaderRaw : IAsset<ShaderRaw> {

    public string Name { get; set; } = "Unnamed";
    public long Id { get; set; }
    public string? Path { get; set; }

    public string source = null!;


    public void Save (string path) {
        File.WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(source));
    }

    public static ShaderRaw? Load (string path) {
        ShaderRaw shaderRaw = new ShaderRaw() {
            Path = path,
            source = Assets.LoadText(path),
        };
        return shaderRaw;
    }


    public void Dispose () {
        
    }

}
