namespace Engine;


[Serializable]
public class Slider_Sensetivity : Slider {
    public Slider_Sensetivity () :
        base("Sensetivity", value: 0.5f, min: 0, max: 1, isInt: false, layout: Layout.Right) { }

    public override void Link () {
        SettingsEngine.Instance.ControlsEngine?.sensetivity = this;
    }
}
