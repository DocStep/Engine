using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Engine;


public class Script : Component {
    public override string Name => nameof(Script) + ": " + GetType().Name;

}
