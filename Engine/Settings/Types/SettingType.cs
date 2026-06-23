using System;
using Newtonsoft.Json;

namespace Engine;


public enum SettingTypes {
    Slider,
    Toggle,
    KeyBind,
    Text,
}

public enum Layout {
    Left,
    Right,
}


[Serializable]
public class SettingType {
    public SettingType (string name, Layout layout = Layout.Left) {
        this.name = name;
        this.layout = layout;
    }

    [JsonIgnore] public Layout layout = Layout.Left;
    [JsonIgnore] public string name;


    public virtual void Apply () { }

    public virtual void Link () { }

}
