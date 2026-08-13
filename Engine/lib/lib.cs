using System.Linq;
using System.Security.Cryptography;

namespace Engine;


public static class lib {

    public static Guid Guid => Guid.NewGuid();
    public static string Uuid => Guid.ToString("N");
    public static long Id => BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);

    private static System.Text.StringBuilder StringBuilder = new System.Text.StringBuilder("", 1000);


    public static int Dictionary_CountAll (IDictionary dictionary) {
        int count = 0;
        foreach (DictionaryEntry itemKV in dictionary)
            if (itemKV.Value is ICollection collection)
                count += collection.Count;
            else return count;
        return count;
    }

    public static string Dictionary_string (IDictionary dictionary) {
        string str = string.Empty;
        foreach (DictionaryEntry itemKV in dictionary)
            str += $"{itemKV.Key} {itemKV.Value}\n";
        return str;
    }
    public static string enum_string (IEnumerable ienum) {
        string str = string.Empty;
        foreach (object item in ienum)
            str += item.ToString() + Environment.NewLine;
        return str;
    }


    public static Dictionary<TEnum, string> Enum_Name_Dict<TEnum> () where TEnum : Enum {
        Dictionary<TEnum, string> entityNames = new();
        TEnum[] enums = (TEnum[])Enum.GetValues(typeof(TEnum));
        foreach (TEnum e in enums) 
            entityNames.Add(e, e.ToString());
        return entityNames;
    }
    public static Dictionary<string, TEnum> Name_Enum_Dict<TEnum> (Dictionary<TEnum, string> dictSrc) where TEnum : Enum {
        Dictionary<string, TEnum> dictDst = new();
        foreach (KeyValuePair<TEnum, string> enumKV in dictSrc)
            dictDst.Add(enumKV.Value, enumKV.Key);
        return dictDst;
    }
    public static Dictionary<TEnum, string> EnumToNameDict_Linq<TEnum> () where TEnum : Enum {
        return Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .ToDictionary(e => e, e => e.ToString());
    }



    public static bool ArrayConstains<T> (T[] array, T value) {
        for (int i = 0; i < array.Length; i++) {
            T t = array[i];
            if (t is null) continue;
            if (t.Equals(value)) return true;
        }
        return false;
    }

    public static T[,] ArrayFill<T> (T[,] array, T value) {
        if (array == null) return null;
        int length0 = array.GetLength(0);
        int length1 = array.GetLength(1);
        for (int x = 0; x < length0; x++) 
            for (int y = 0; y < length1; y++) 
                array[x, y] = value;
        return array;
    }
    public static T[,,] ArrayFill<T> (T[,,] array, T value) {
        if (array == null) return null;
        int length0 = array.GetLength(0);
        int length1 = array.GetLength(1);
        int length2 = array.GetLength(2);
        for (int x = 0; x < length0; x++) 
            for (int y = 0; y < length1; y++) 
                for (int z = 0; z < length2; z++) 
                    array[x, y, z] = value;
        return array;
    }


    public static float ArrayAvg (float[,] array) {
        if (array == null) return 0;
        float avg = 0;
        int length0 = array.GetLength(0);
        int length1 = array.GetLength(1);
        for (int x = 0; x < length0; x++)
            for (int y = 0; y < length1; y++)
                avg += array[x, y];
        avg /= length0*length1;
        return avg;
    }
    public static float ArrayAvg (float[,,] array) {
        if (array == null) return 0;
        float avg = 0;
        int length0 = array.GetLength(0);
        int length1 = array.GetLength(1);
        int length2 = array.GetLength(2);
        for (int x = 0; x < length0; x++)
            for (int y = 0; y < length1; y++)
                for (int z = 0; z < length2; z++)
                    avg += array[x, y, z];
        avg /= length0*length1*length2;
        return avg;
    }

    

    public static string UID (string name) {
        string input = name;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
        using (MD5 md5 = MD5.Create()) {
            byte[] hash = md5.ComputeHash(bytes);
            return new Guid(hash).ToString();
        }
    }

    public static void DirectoryExists (string path) {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

}
