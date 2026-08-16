using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public class SunLight : LightSource {
    public override string Name => nameof(SunLight);

    [Hide] public Quaternion Rotation => owner.Transform.Rotation;


    public override void OnAdd () {
        //owner.Transform.Rotation = Mathf.DirectionToQuaternion(Constants.sunLightDir);
        owner.Transform.RotationEuler = Constants.sunLightEuler;
        Lighting.RegisterLightSource(this);
    }
    public override void OnRemove () {
        Lighting.UnregisterLightSource(this);
    }

}
