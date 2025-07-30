using eyecandy;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace opengl_mp4_test;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("\nOpenGL MP4 Playback Test\n\n");

        var windowConfig = new EyeCandyWindowConfig();
        windowConfig.OpenTKNativeWindowSettings.Title = "GLMP4";
        windowConfig.OpenTKNativeWindowSettings.ClientSize = (960, 540);
        windowConfig.StartFullScreen = false;
        windowConfig.OpenTKNativeWindowSettings.APIVersion = new Version(4, 5);

        var win = new Win(windowConfig);
        win.Focus();
        win.Run();
        win.Dispose();
    }
}
