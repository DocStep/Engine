using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Engine;


public static class lib {
    //public static FastNoiseLite Noise = new FastNoiseLite();

    public const float TAU = 6.2831855f;

    public static Random random = new Random(25565);
    public static float R () => (float)random.NextDouble();
    public static int R (int max) => random.Next(0, max);
    public static int R (int min, int max) => random.Next(min, max);
    public static float R (float min, float max) => min + (float)random.NextDouble()*(max - min);

    public static StringBuilder StringBuilder = new StringBuilder("", 1000);


    public static float Booly1 (this float f, bool b) {
        return b ? f : 1;
    }
    public static float Booly05 (this float f, bool b) {
        return b ? f : 0.5f;
    }
    public static float Booly0 (this float f, bool b) {
        return b ? f : 0;
    }


    public static int Dictionary_countAll (IDictionary dictionary) {
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
        foreach (var item in ienum)
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
        foreach (var enumKV in dictSrc)
            dictDst.Add(enumKV.Value, enumKV.Key);
        return dictDst;
    }
    public static Dictionary<TEnum, string> EnumToNameDict_Linq<TEnum> () where TEnum : Enum {
        return Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .ToDictionary(e => e, e => e.ToString());
    }

    /// <summary> 1->0 = 0 </summary>
    public static bool Implies (bool a, bool b) => !a || b;
    /// 0 0 1
    /// 0 1 1
    /// 1 0 0
    /// 1 1 1


    public static float easeInQuad (float x) {
        return x*x;
    }
    public static float easeInCubic (float x) {
        return x*x*x;
    }
    public static float easeInSine (float x) {
        return 1 - MathF.Cos(0.5f*(x*MathF.PI));
    }

    public static float easeOutQuad (float x) {
        return 1 - (1 - x)*(1 - x);
    }
    public static float easeOutCirc (float x) {
        return MathF.Sqrt(1f - (x - 1f)*(x - 1f));
    }

    public static float easeInOutSine (float x) {
        return x < 0.5f ?
            4f*x*x*x :
            1f - MathF.Pow(-2f*x + 2f, 3f)*0.5f;
    }
    public static float easeInOutCubic (float x) {
        return x < 0.5f ? 
            4f*x*x*x : 
            1f - MathF.Pow(-2f*x + 2f, 3f)*0.5f;
    }



    public static float Remap01 (float value, float start, float end) => (value - start)/(end - start);

    public static float Remap (float value, float startSrc, float endSrc, float startDst, float endDst) {
        return startDst + (value - startSrc)*(endDst - startDst)/(endSrc - startSrc);
    }


    public static bool ArrayConstains<T> (T[] array, T value) {
        for (int i = 0; i < array.Length; i++) 
            if (array[i].Equals(value)) return true;
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

    public static float Saturate (float value) => value < 0 ? 0 : value;
    public static int Saturate (int value) => value < 0 ? 0 : value;
    public static float Saturate (float value, float edge) => value < edge ? edge : value;
    public static float Saturate1 (float value) => value < 1 ? 1 : value;
    public static float SaturateNegative (float value) => value < 0 ? value : 0;

    public static int Sum (params int[] values) {
        if (values == null || values.Length == 0) return 0;
        int sum = 0;
        foreach (int v in values) sum += v;
        return sum;
    }
    public static float Sum (params float[] values) {
        if (values == null || values.Length == 0) return 0f;
        float sum = 0f;
        foreach (float v in values) sum += v;
        return sum;
    }
    public static float Avg (params float[] values) {
        if (values == null || values.Length == 0) return 0f;
        return Sum(values)/values.Length;
    }
    public static bool InRadius (float x, float y, float radius) {
        return x*x + y*y < radius*radius;
    }
    public static bool InSquare (float x, float y, float radius) {
        return -radius <= x && x <= radius && -radius <= y && y <= radius;
    }


    public static string UID (string name) {
        string input = name;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
        using (MD5 md5 = MD5.Create()) {
            byte[] hash = md5.ComputeHash(bytes);
            return new System.Guid(hash).ToString();
        }
    }

    public static void DirectoryExists (string path) {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

}
