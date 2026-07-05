using Newtonsoft.Json;

namespace Engine;


[Serializable]
public abstract class SettingsGroup {
    public SettingsGroup (string name) { this.name = name; }

    [JsonIgnore] public string name;
    [JsonIgnore] public List<SettingType> settings = new List<SettingType>();
    
    public virtual void ToSpawnList () { }

}
