using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


//[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
public class ChangeStep (float step = 1f) : Attribute {
    public float Step = step;
}
