using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Input;


public class InputsGroup {
    public InputsGroup Clone () {
        return (InputsGroup)MemberwiseClone();
    }
    public InputsGroup (List<Keys> Keys, bool hidden = false) {
        this.Keys = Keys;
        this.hidden = hidden;
    }
    //public ActionStateEnum KeyState;
    public List<Keys> Keys;
    public bool hidden = false;

    public bool pressedDown;
    public bool pressed;
    public bool pressedUp;
}
