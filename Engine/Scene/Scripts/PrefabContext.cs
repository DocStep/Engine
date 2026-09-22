namespace Engine;


public class PrefabContext {

    /// <summary>Save-time: any GameObject, Transform or Component instance -> its $id in this file.</summary>
    public readonly Dictionary<object, int> WriteIds = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
 
    /// <summary>Load-time: $id -> the GameObject/Transform/Component created for it.</summary>
    public readonly Dictionary<int, object> ReadObjects = new Dictionary<int, object>();

    /// Save-time only: every IAsset with no Path, in the order it was first seen — these get
    /// their own block in Objects[] instead of being written as a path string.
    public readonly List<IAsset> InlineAssets = new List<IAsset>();

}
