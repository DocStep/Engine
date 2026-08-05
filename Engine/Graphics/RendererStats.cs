namespace Engine;


public struct RendererStats () {

    public float Latency = 0f;

    public int DrawCalls = 0;
    public int PostProccessCalls = 0;
    public int DrawCallsUI = 0;

    public long Frame = 0;
    public Vector2 WindowSize = default;
    public Vector2 SceneSize = default;

}
