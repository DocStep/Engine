namespace Engine;


public interface IAwake {
    public bool Enabled { get; set; }
    void Awake();
}
