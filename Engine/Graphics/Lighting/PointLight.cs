using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public class PointLight : LightSource {
    public override string Name => nameof(PointLight);

    public float Range = Constants.PointLight_Radius;


}
