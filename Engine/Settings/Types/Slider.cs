using Newtonsoft.Json;

namespace Engine;


[Serializable]
public abstract class Slider : SettingType {
    public Slider (string name, float value, float min, float max, bool isInt, Layout layout) : 
        base(name, layout) {
        this.min = min;
        this.max = max;
        this.isInt = isInt;
        this.value = value;
    }


    public float value = 0.5f;
    [JsonIgnore] public virtual float Value { get => value; set => this.value = value; }
    [JsonIgnore] public float min = 0;
    [JsonIgnore] public float max = 1;
    [JsonIgnore] public bool isInt = false;



    public void SetValue (float value) {
        Value = value;
        SettingsEngine.SettingsSave();
    }

    public virtual void onChange (float value) {
        if (this.value == value) return;

        SetValue(value);
    }


}
