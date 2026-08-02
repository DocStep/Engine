namespace Engine;


public struct EngineStats () {

    /// Update
    public float LatencyFull = 0f;
    public float LatencyUpdate = 0f;
    public float LatencyRender = 0f;

    /// FixedUpdate
    public float LatencyFixedUpdate = 0f;
    public float LatencyPhysics = 0f;
    public float LatencyComponents = 0f;

    public int ComponentsCalls = 0;

}
