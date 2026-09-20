namespace Engine;


public interface IUpdateAtFreeze {
    public bool Enabled { get; set; }
    void Update ();
}
