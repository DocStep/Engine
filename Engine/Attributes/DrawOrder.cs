using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class OrderAttribute (int order) : Attribute {
    public int Order = order;
}
