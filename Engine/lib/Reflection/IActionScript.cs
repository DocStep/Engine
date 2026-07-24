namespace Engine;


public interface IActionScript { }

public interface IActionScript_Start : IActionScript {
    void OnScriptAction_Start ();
}

public interface IActionScript_FixedUpdate : IActionScript {
    void OnScriptAction_FixedUpdate ();
}

public interface IActionScript_Update : IActionScript {
    void OnScriptAction_Update ();
}

public interface IActionScript_Exit : IActionScript {
    void OnScriptAction_Exit ();
}
