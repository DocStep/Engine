namespace Engine;


public interface ISavable {

    public abstract void PreSave ();
    //public abstract JObj ToJObj ();
    public abstract void PostLoad ();

    public static T? ToComponent<T> (JObj jObj) where T : Component {
        return jObj.Data as T;
    }

}
