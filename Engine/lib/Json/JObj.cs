namespace Engine;


[Serializable]
public class JObj {
    public JObj (string Type, object Data) {
        this.Type = Type;
        this.Data = Data;
    }
    public JObj (object Data) {
        this.Type = Data.GetType().Name;
        this.Data = Data;
    }

    public string Type = null!;
    public object Data = null!;

}
