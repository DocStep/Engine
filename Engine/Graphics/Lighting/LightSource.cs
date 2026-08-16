using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public class LightSource : Component {
    public override string Name => nameof(SunLight);

    [Hide] public Vector3 Position => gameObject.Transform.Position;

    public Vector3 Color = Constants.Light_Color;
    public float Intensity = Constants.Light_Intensity;


    public override void OnAdd () {
        Lighting.RegisterLightSource(this);
    }
    public override void OnRemove () {
        Lighting.UnregisterLightSource(this);
    }

}
