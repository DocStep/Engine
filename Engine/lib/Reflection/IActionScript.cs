namespace Engine;


public interface IActionScript { }

public interface IActionScript_Start : IActionScript {
    void Start_AS ();
}

public interface IActionScript_FixedUpdate : IActionScript {
    void FixedUpdate_AS ();
}

public interface IActionScript_Update : IActionScript {
    void Update_AS ();
}

public interface IActionScript_Exit : IActionScript {
    void Exit_AS ();
}
