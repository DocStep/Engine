using System.Linq;
using Engine;

namespace Engine;


public static class Windows {

    //public static Silk.NET.Windowing.IWindow Window = null!;
    //public static Silk.NET.Input.IInputContext Input = null!;

    public static Silk.NET.Windowing.IWindow WindowCreate () {
        Silk.NET.Windowing.IWindow Window = Silk.NET.Windowing.Window.Create(options);

        IEnumerable<Silk.NET.Windowing.IMonitor> monitors = Silk.NET.Windowing.Monitor.GetMonitors(Window);
        Silk.NET.Windowing.IMonitor monitor = monitors.First();
        Silk.NET.Maths.Vector2D<int> screenSize = monitor.VideoMode.Resolution ?? new Silk.NET.Maths.Vector2D<int>(1920, 1080);
        Window.Position = new Silk.NET.Maths.Vector2D<int>((screenSize.X - options.Size.X)/2, (screenSize.Y - options.Size.Y)/2);

        Engine.Instance.de_Update_Engine += Update;
        return Window;
    }

    public static Silk.NET.Windowing.WindowOptions options = Silk.NET.Windowing.WindowOptions.Default with {
        Size = new Silk.NET.Maths.Vector2D<int>(1280, 720),
        Title = "Engine",
        VSync = false,
        WindowBorder = Silk.NET.Windowing.WindowBorder.Resizable,
    };


    private static void Update () {
        if (Input.Inputs.Actions[Input.Inputs.FullscreenSwitch].pressedDown) {
            Log.log("FullscreenSwitch");
            if (Engine.Window.WindowState == Silk.NET.Windowing.WindowState.Fullscreen) {
                Engine.Window.WindowState = Silk.NET.Windowing.WindowState.Normal;
            } else {
                Engine.Window.WindowState = Silk.NET.Windowing.WindowState.Fullscreen;
            }
        }

        if (Input.Inputs.Actions[Input.Inputs.Exit].pressed) Engine.Window.Close();
    }

}