namespace Engine;


[Serializable]
public class TextField : SettingType {
    public TextField (string name, string text = "", Layout layout = Layout.Left) : base(name, layout) {
        this.text = text;
    }

    public string text = string.Empty;

    public virtual void onChange (string text) {
        if (this.text != text) return;

        this.text = text;
    }

}
