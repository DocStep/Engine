using System;
using System.Collections.Generic;
using Engine.Input;

namespace Engine;


public static class DataEngine {

    public static float global_audio_Mult = 1f;


    public static Dictionary<string, InputsGroup> InputsData = new Dictionary<string, InputsGroup>() {
        [Inputs.MoveUp] = new([Keys.Space, Keys.E], hidden: true),
        [Inputs.MoveDown] = new([Keys.C, Keys.Q], hidden: true),
    };


}
