using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


//[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class WrapFloat : Attribute {
    public float Min;
    public float Max;
    public const float step = 1f;
    public WrapFloat (float min, float max) {
        Min = min; Max = max;
    }

    public float Update (float value) {
        value = value % 360f;
        if (value < 0f) value += 360f;
        return value;
    }

}
