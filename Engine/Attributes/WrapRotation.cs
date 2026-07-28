using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


//[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class WrapRotation : Attribute {
    public float Min;
    public float Max;
    public const float Step = 1f;
    public WrapRotation (float min, float max) {
        Min = min; Max = max;
    }
}
