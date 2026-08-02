namespace Engine;


public struct RendererStats () {

    public float Latency = 0f;

    public int DrawCalls = 0;
    public int PostProcessCalls = 0;
    public int DrawCallsUI = 0;

    public long Frame = 0;

}
