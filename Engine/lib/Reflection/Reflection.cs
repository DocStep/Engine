using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Linq;

namespace Engine;


public class Reflection : Singleton<Reflection> {

    public List<IActionScript> Scripts = new List<IActionScript>();


    protected override void Init () {
        /*Type[] types = FindAllSubclasses<IActionScript>();
        for (int t = 0; t < types.Length; t++) {
            IActionScript? script = Activator.CreateInstance(types[t]) as IActionScript;
            if (script is null) continue;

            Scripts.Add(script);
            if (script is IActionScript_Start action_start) {
                de_Actions_Start += action_start.OnScriptAction_Start;
            }
            if (script is IActionScript_FixedUpdate action_fixedUpdate) {
                de_Actions_FixedUpdate += action_fixedUpdate.OnScriptAction_FixedUpdate;
            }
            if (script is IActionScript_Update action_update) {
                de_Actions_Update += action_update.OnScriptAction_Update;
            }
            if (script is IActionScript_Exit action_exit) {
                de_Actions_Exit += action_exit.OnScriptAction_Exit;
            }
        }*/
    }

    public static Type[] FindAllSubclasses<T> (bool doAbstract = false) {
        Type baseType = typeof(T);
        Assembly? assembly = Assembly.GetAssembly(baseType);
        if (assembly is null) return [];

        Type[] types = assembly.GetTypes();
        Type[] subClasses = types.Where(type => type.IsSubclassOf(baseType) && (doAbstract || !type.IsAbstract)).ToArray();
        return subClasses;
    }


    /*public Func<IEnumerator>? de_Actions_Start = null;
    public Func<IEnumerator>? de_Actions_FixedUpdate = null;
    public Func<IEnumerator>? de_Actions_Update = null;
    public Func<IEnumerator>? de_Actions_Exit = null;*/


    /*public IEnumerator RunScriptActions_Start () {
        if (de_Actions_Start is null) yield break;

        foreach (var delegat in de_Actions_Start.GetInvocationList()) {
            var func = (Func<IEnumerator>)delegat;
            yield return func();
        }
    }
    public IEnumerator RunScriptActions_FixedUpdate () {
        if (de_Actions_FixedUpdate is null) yield break;

        foreach (var delegat in de_Actions_FixedUpdate.GetInvocationList()) {
            var func = (Func<IEnumerator>)delegat;
            yield return func();
        }
    }
    public IEnumerator RunScriptActions_Update () {
        if (de_Actions_Update is null) yield break;

        foreach (var delegat in de_Actions_Update.GetInvocationList()) {
            var func = (Func<IEnumerator>)delegat;
            yield return func();
        }
    }
    public IEnumerator RunScriptActions_Exits () {
        if (de_Actions_Exit is null) yield break;

        foreach (var delegat in de_Actions_Exit.GetInvocationList()) {
            var func = (Func<IEnumerator>)delegat;
            yield return func();
        }
    }*/


    public List<T> FindAll<T> () {
        return ScriptsCache<T>.FindAll(this);
    }

}
