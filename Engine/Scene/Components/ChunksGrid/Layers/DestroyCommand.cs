namespace Engine;


public readonly struct DestroyCommand {
    public readonly GameObject Target;
    public DestroyCommand (GameObject target) {
        Target = target;
    }
    public void Execute () {
        Target.Destroy();
    }
}
