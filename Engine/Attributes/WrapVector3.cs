using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


//[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class WrapVector3 : Attribute {
    public float Min;
    public float Max;
    public const float Step = 1f;
    public WrapVector3 (float min, float max) {
        Min = min; Max = max;
    }
}
