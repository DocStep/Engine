using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public class SunLight : LightSource {

    [Hide] public Quaternion Rotation => gameObject.Transform.Rotation;

}
