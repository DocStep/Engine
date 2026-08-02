using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


//[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class ChangeStep : Attribute {
    public ChangeStep (float step = 1f) {
        Step = step;
    }
    public float Step = 1f;
}
