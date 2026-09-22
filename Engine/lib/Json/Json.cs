using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Engine;


public static class Json {

    public static void Init () {
        KnownTypes.Clear();
        KnownTypes.AddRange(JsonEngine.jsonTypes);

        KnownTypesBinder = new KnownTypesBinder() {
            KnownTypes = KnownTypes,
        };

        Converters = new List<JsonConverter> {

        };

        JsonSettings_General = new JsonSerializerSettings {
            //TypeNameHandling = TypeNameHandling.All,
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            //TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            SerializationBinder = KnownTypesBinder,
            Converters = Converters,
        };
        JsonSettings_Private = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            SerializationBinder = KnownTypesBinder,
            ContractResolver = new PrivateFieldsResolver(),
            Converters = Converters,
        };
        JsonSettings_Settings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            SerializationBinder = KnownTypesBinder,
            ContractResolver = new PrivateFieldsResolver(),
            Converters = Converters,
        };
    }

    
    public static readonly List<Type> KnownTypes = new List<Type>();

    public static KnownTypesBinder? KnownTypesBinder;
    public static List<JsonConverter>? Converters;

    public static JsonSerializerSettings? JsonSettings_General;
    public static JsonSerializerSettings? JsonSettings_Private;
    public static JsonSerializerSettings? JsonSettings_Settings;



    static readonly Dictionary<string, object> FileLocks = new();

    static object GetLock (string path) {
        if (!FileLocks.TryGetValue(path, out object? locker)) {
            locker = new object();
            FileLocks[path] = locker;
        }
        return locker;
    }


    public static T? Read<T> (string path, bool withPrivate = false) {
        string json;
        lock (GetLock(path)) {
            json = File.ReadAllText(path);
        }
        return JsonConvert.DeserializeObject<T>(json, withPrivate ? JsonSettings_Private : JsonSettings_General);
    }
    public async static System.Threading.Tasks.Task<T> ReadAsync<T> (string path, bool withPrivate = false) {
        return await Json.ReadAsync<T>(path, withPrivate);
    }

    public static T? Convert<T> (string json, bool withPrivate = false) {
        return JsonConvert.DeserializeObject<T>(json, withPrivate ? JsonSettings_Private : JsonSettings_General);
    }
    //public static void JsonRead<T> (string path, out T output, bool withPrivate = false) {
    //    string json = File.ReadAllText(path);
    //    output = JsonConvert.DeserializeObject<T>(json, withPrivate ? JsonSettings_Private : JsonSettings_General);
    //}

    public static void Write (string path, object obj, Formatting formatting = Formatting.Indented, bool withPrivate = false) {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        string json = JsonConvert.SerializeObject(obj, formatting, withPrivate ? JsonSettings_Private : JsonSettings_General);
        lock (GetLock(path)) {
            File.WriteAllText(path, json);
        }
    }


    public static T? ReadSettings<T> (string path) {
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json, JsonSettings_Settings);
    }
    public static void WriteSettings (string path, SettingsEngine settings) {
        string json = JsonConvert.SerializeObject(settings, Formatting.Indented, JsonSettings_Settings);
        File.WriteAllText(path, json);
    }

    /*public static void JsonWriteSettings (string path, object obj, bool withPrivate = false) {
        string json = JsonConvert.SerializeObject(obj, Formatting.Indented, withPrivate ? JsonSettings_Private : JsonSettings_General);
        File.WriteAllText(path, json);
    }*/



    //public static void JsonWriteTask (string path, object obj, Formatting formatting = Formatting.Indented, bool wait = false) {
    //    if (wait) {
    //        Task.Run(() => {
    //            string json = JsonConvert.SerializeObject(obj, formatting, lib.JsonSettings);
    //            File.WriteAllText(path, json);
    //        });
    //    } else {
    //        string json = JsonConvert.SerializeObject(obj, formatting, lib.JsonSettings);
    //        File.WriteAllText(path, json);
    //    }
    //}

}
