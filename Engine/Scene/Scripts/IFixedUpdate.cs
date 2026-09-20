namespace Engine;


public interface IFixedUpdate {
    public bool Enabled { get; set; }
    void FixedUpdate ();
}
