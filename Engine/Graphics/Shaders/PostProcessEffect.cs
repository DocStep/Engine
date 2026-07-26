using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Graphics;


public abstract class PostProcessEffect {

    public abstract void Apply (uint inputTexture);

}
