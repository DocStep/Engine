namespace Engine;


public struct RendererGLStats () {

    public int ShaderCompiled { get; private set; } = 0;
    public int Shaders_Error { get; private set; } = 0;

    public void RecordCompile (int status) {
        if (status != 0) ShaderCompiled++;
        else Shaders_Error++;
    }

}
