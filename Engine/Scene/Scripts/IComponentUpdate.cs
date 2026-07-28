namespace Engine;


public interface IComponentUpdate {

    public void Update();
    public bool Enabled { get; set; }

}
