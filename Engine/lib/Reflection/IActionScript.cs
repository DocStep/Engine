namespace Engine;


public interface IActionScript {

}

public interface IActionScript_Start {
    public IEnumerator OnScriptAction_Start ();
}

public interface IActionScript_FixedUpdate {
    public IEnumerator OnScriptAction_FixedUpdate ();
}

public interface IActionScript_Update {
    public IEnumerator OnScriptAction_Update ();
}

public interface IActionScript_Exit {
    public IEnumerator OnScriptAction_Exit ();
}
