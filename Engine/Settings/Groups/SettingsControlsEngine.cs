using System;

namespace Engine;


[Serializable]
public class SettingsControlsEngine : SettingsGroup {
    public SettingsControlsEngine () : base("Controls") { SettingsControlsEngine.Instance = this; }
    public static SettingsControlsEngine Instance = null!;

    public Slider_Sensetivity sensetivity = new Slider_Sensetivity();

    public override void ToSpawnList () {
        settings.Add(sensetivity);
    }

}
