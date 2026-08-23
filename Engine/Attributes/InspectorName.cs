using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


public class InspectorName (string name) : Attribute {
    public string Name = name;
}
