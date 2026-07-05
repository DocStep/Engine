namespace Engine;


[Serializable]
public class SettingsSoundEngine : SettingsGroup {
    public SettingsSoundEngine () : base("Sound") { SettingsSoundEngine.Instance = this; }
    public static SettingsSoundEngine Instance = null!;

    public Slider_Volume volume = new Slider_Volume();


    public override void ToSpawnList () {
        settings.Add(volume);
    }

}
