namespace Engine;


[Serializable]
public class Slider_Volume : Slider {
    public Slider_Volume () :
        base("Volume", value: 0.5f, min: 0, max: 1, isInt: false, layout: Layout.Left) {

    }


    public override void onChange (float value) {
        base.onChange(value);

    }


    public override void Link () {
        SettingsEngine.Instance.SoundEngine?.volume = this;
    }
}
