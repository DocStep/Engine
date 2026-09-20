namespace Engine;


public interface IStart {
    public bool Enabled { get; set; }
    void Start();
}
