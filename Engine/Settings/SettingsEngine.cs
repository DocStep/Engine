using Newtonsoft.Json;

namespace Engine;


[Serializable]
public class SettingsEngine {
    [JsonConstructor]
    public SettingsEngine (bool json) {
        SettingsEngine.Instance = this;
    }
    public SettingsEngine () {
        SettingsEngine.Instance = this;

        ControlsEngine = new SettingsControlsEngine();
        SoundEngine = new SettingsSoundEngine();
        GraphicsEngine = new SettingsGraphicsEngine();
    }
    [JsonIgnore] public static SettingsEngine Instance = null!;

    //public static Type SettingsType;

    [JsonIgnore] public List<SettingsGroup> Groups = new List<SettingsGroup>();

    public SettingsControlsEngine? ControlsEngine = null;
    public SettingsSoundEngine? SoundEngine = null;
    public SettingsGraphicsEngine? GraphicsEngine = null;


    public virtual SettingsEngine SavePack () {
        return this;
    }
    public virtual SettingsEngine LoadUnpack () {
        return this;
    }



    public virtual SettingsEngine ToSpawnList () {
        Groups.Add(ControlsEngine);
        Groups.Add(SoundEngine);
        Groups.Add(GraphicsEngine);
        return this;
    }
    public void ToSpawnGroupsList () {
        for (int g = 0; g < Groups.Count; g++)
            Groups[g]?.ToSpawnList();
    }

    [JsonIgnore]public static SettingsEngine Settings = null!;

    public static void SettingsInit<T> () where T : SettingsEngine, new() {
        SettingsEngine.SettingsLoad<T>();
        if (SettingsEngine.Settings == null) 
            SettingsEngine.SettingsCreate(new T());
    }
    static bool SettingsLoad<T> () where T : SettingsEngine, new() {
        if (!File.Exists(SettingsEngine.settingsPath)) return false;

        SettingsEngine? Settings = Json.ReadSettings<T>(SettingsEngine.settingsPath);
        if (Settings is null) return false;

        SettingsEngine.Settings = Settings.LoadUnpack();
        Log.log($"Inited (Loaded): {SettingsEngine.Settings}");
        return true;
    }
    static void SettingsCreate (SettingsEngine Settings) {
        SettingsEngine.Settings = Settings;
        SettingsEngine.SettingsSave();
        Log.log($"Inited (Created): {SettingsEngine.Settings}");
    }


    [JsonIgnore] public static readonly string settingsName = "Settings";
    [JsonIgnore] public static string settingsPath => Path.Combine(Engine.savesFolder, $"{settingsName}.json");


    public static void SettingsSave () {
        //Debug.Log($"SettingsSave");
        Json.WriteSettings(settingsPath, SettingsEngine.Settings.SavePack());
    }

}
