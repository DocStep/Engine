namespace Engine;


public interface ISerializationCallbackReceiver {
    void OnBeforeSerialize ();
    void OnAfterDeserialize ();
}
