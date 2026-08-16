using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public class SunLight : LightSource {
    public override string Name => nameof(SunLight);

    [Hide] public Quaternion Rotation => gameObject.Transform.Rotation;

}
