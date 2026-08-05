using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


//[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class WrapFloat (float min, float max) : Attribute {
    public float Min = min;
    public float Max = max;

    public float Update (float value) {
        value = value % 360f;
        if (value < 0f) value += 360f;
        return value;
    }

}
