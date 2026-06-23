using System;
using Newtonsoft.Json;

namespace Engine;


[Serializable]
public abstract class Toggle : SettingType {
    public Toggle (string name, bool isOn = false, Layout layout = Layout.Left) : base(name, layout) {
        this.isOn = isOn;
    }

    //[JsonIgnore] public UnityEngine.UI.Toggle tg_toggle;

    public bool isOn = false;


    public void SetValue (bool isOn) {
        this.isOn = isOn;
        SettingsEngine.SettingsSave();
    }

    public virtual bool onChange (bool isOn) {
        if (this.isOn == isOn) return false;

        SetValue(isOn);
        Apply();
        return true;
    }


}
