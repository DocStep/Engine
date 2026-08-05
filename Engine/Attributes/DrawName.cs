using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


public class DrawName (string name) : Attribute {
    public string Name = name;
}
