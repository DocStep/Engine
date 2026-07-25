using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;

public interface AttributeVector3 {
    Vector3 Update (Vector3 v3);
}

[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class AttributeClampRotation : Attribute, AttributeVector3 {
    public float Min;
    public float Max;
    public const float step = 1f;
    public AttributeClampRotation (float min, float max) {
        Min = min; Max = max;
    }

    public Vector3 Update (Vector3 v3) {
        v3.X = v3.X%360f;
        if (v3.X < 0f) v3.X += 360f;
        v3.Y = v3.Y%360f;
        if (v3.Y < 0f) v3.Y += 360f;
        v3.Z = v3.Z%360f;
        if (v3.Z < 0f) v3.Z += 360f;
        return v3;
    }

}
