namespace Engine;


public struct RendererGLStats () {

    public int Shaders_Compiled { get; private set; } = 0;
    public int Shaders_Error { get; private set; } = 0;

    public void RecordCompile (int status) {
        if (status != 0) Shaders_Compiled++;
        else Shaders_Error++;
    }

}
