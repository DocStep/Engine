namespace Engine;


[Serializable]
public class SettingsGraphicsEngine : SettingsGroup {
    public SettingsGraphicsEngine () : base("Graphics") { SettingsGraphicsEngine.Instance = this; }
    public static SettingsGraphicsEngine Instance = null!;

    public Toggle_PostProcessing postProcessing = new Toggle_PostProcessing();


    public override void ToSpawnList () {
        settings.Add(postProcessing);
    }

}
