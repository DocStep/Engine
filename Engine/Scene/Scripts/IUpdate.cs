namespace Engine;


public interface IUpdate {
    public bool Enabled { get; set; }
    void Update();
}
