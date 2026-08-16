namespace Engine;


public class ReflectionActionScripts : Singleton<ReflectionActionScripts> {

    protected override void Init () {
        Type[] types = Reflection.FindAllSubclasses<IActionScript>(doAbstract: true);
        for (int t = 0; t < types.Length; t++) {
            IActionScript? script = Activator.CreateInstance(types[t]) as IActionScript;
            if (script is null) {
                Log.log("[ReflectionActionScripts]", "Failed to construct", types[t], LogType.warning);
                continue;
            }

            Scripts.Add(script);
            if (script is IActionScript_Start action_start) {
                de_Actions_Start += Wrap(action_start.Start_AS, types[t]);
            }
            if (script is IActionScript_FixedUpdate action_fixedUpdate) {
                de_Actions_FixedUpdate += Wrap(action_fixedUpdate.FixedUpdate_AS, types[t]);
            }
            if (script is IActionScript_Update action_update) {
                de_Actions_Update += Wrap(action_update.Update_AS, types[t]);
            }
            if (script is IActionScript_Exit action_exit) {
                de_Actions_Exit += Wrap(action_exit.Exit_AS, types[t]);
            }

            Log.log("[ReflectionActionScripts]", "Registered:", types[t]);
        }
    }

    public List<IActionScript> Scripts = new List<IActionScript>();
    public Action? de_Actions_Start = null;
    public Action? de_Actions_FixedUpdate = null;
    public Action? de_Actions_Update = null;
    public Action? de_Actions_Exit = null;


    public List<T> FindScriptsAll<T> () {
        return ReflectionActionScriptsCache<T>.FindAll(this);
    }


    static Action Wrap (Action action, Type sourceType) {
        return () => {
            try {
                action();
            } catch (Exception ex) {
                Log.log($"{sourceType.Name} threw during script action: {ex}");
            }
        };
    }

}