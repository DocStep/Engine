namespace Engine;


public interface IComponentUpdate {

    void Update();
    public bool Enabled { get; set; }

}
