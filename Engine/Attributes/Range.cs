using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


//[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class Range (float min, float max) : Attribute {
    public float Min = min;
    public float Max = max;

    public float Update (float value) {
        return Mathf.Clamp(value, Min, Max);
    }

}
