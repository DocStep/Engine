namespace Engine;


public class PrefabContext {
    /// <summary>Save-time: any GameObject, Transform or Component instance -> its $id in this file.</summary>
    public readonly Dictionary<object, int> WriteIds = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
 
    /// <summary>Load-time: $id -> the GameObject/Transform/Component created for it.</summary>
    public readonly Dictionary<int, object> ReadObjects = new Dictionary<int, object>();
}

