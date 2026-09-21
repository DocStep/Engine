namespace Engine;


public class JsonEngine {

    public static HashSet<Type> jsonTypes = new HashSet<Type>() {
        typeof(SettingsEngine), typeof(SettingsEngineHelp),
        typeof(SettingsControlsEngine), typeof(SettingsSoundEngine), typeof(SettingsGraphicsEngine),
        typeof(Slider), typeof(Toggle), typeof(Keybinds), typeof(Keybinds), typeof(TextField),
        typeof(Slider_Sensetivity), typeof(Slider_Volume), typeof(Toggle_PostProcessing),
    };

}
