using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public class LightSource : Component {
    public override string Name => nameof(SunLight);

    [Hide] public Vector3 Position => gameObject.Transform.Position;

    public Vector3 Color = Constants.sunLightColor;
    public float Intensity = Constants.sunLightIntensity;


    public virtual void ApplyLightSource () {

    }

}
