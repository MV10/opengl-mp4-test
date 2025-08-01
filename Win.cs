
using eyecandy;
using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace opengl_mp4_test;

public class Win : BaseWindow, IDisposable
{
    // just a quad from two triangles that cover the whole display area
    private readonly float[] vertices =
    {
        // position          texture coords
         1.0f,  1.0f, 0.0f,   1.0f, 1.0f,     // top right
         1.0f, -1.0f, 0.0f,   1.0f, 0.0f,     // bottom right
        -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,     // bottom left
        -1.0f,  1.0f, 0.0f,   0.0f, 1.0f      // top left
    };

    private readonly uint[] indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    private int ElementBufferObject;
    private int VertexBufferObject;
    private int VertexArrayObject;

    private Stopwatch Clock = new();

    private VideoTexture MP4 = new();

    public Win(EyeCandyWindowConfig windowConfig)
        : base(windowConfig, createShaderFromConfig: false)
    {
        // location of FFmpeg DLLs from gyan.dev; see https://github.com/radek-k/FFMediaToolkit?tab=readme-ov-file#setup
        FFmpegLoader.FFmpegPath = @"C:\Source\_dev_utils_standalone\ffmpeg_20250303_v7.1.1_bin";

        var vertexShaderPathname = "shader.vert";
        var fragmentShaderPathname = "shader.frag";
        Shader = new(vertexShaderPathname, fragmentShaderPathname);
        if (!Shader.IsValid) Environment.Exit(-1);

        Clock.Start();
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // Audio is not supported
        var options = new MediaOptions
        {
            StreamsToLoad = MediaMode.Video
        };

        // Included in this repository
        MP4.Load(@"example.mp4", options);

        // Examples from MV10\volts-laboratory
        //MP4.Load(@"C:\Source\volts-laboratory\textures\Shadertoy LustreCreme.ogv", options);
        //MP4.Load(@"C:\Source\volts-laboratory\textures\Shadertoy Britney.webm", options);
        //MP4.Load(@"C:\Source\volts-laboratory\textures\Shadertoy VanDamme.webm", options);
        //MP4.Load(@"C:\Source\volts-laboratory\textures\Shadertoy GoogleChrome.ogv", options);
        //MP4.Load(@"C:\Source\volts-laboratory\textures\traffic.mp4", options);
        //MP4.Load(@"C:\Source\volts-laboratory\textures\dancer.mp4", options);
        //MP4.Load(@"C:\Source\volts-laboratory\textures\costume.mp4", options);

        if (MP4.IsLoaded && MP4.File.HasVideo)
        {
            Console.WriteLine($"Video file: {MP4.File.Info.FilePath}");
            Console.WriteLine($"Dimensions: {MP4.Width}x{MP4.Height}");
            Console.WriteLine($"Duration: {MP4.Duration.TotalSeconds} seconds");
            Console.WriteLine($"Bitrate: {MP4.File.Info.Bitrate / 1000.0} kb/s");
            var info = MP4.File.Video.Info;
            Console.WriteLine($"Frame rate: {info.AvgFrameRate} fps ({(info.IsVariableFrameRate ? "average" : "constant")})");
            Console.WriteLine($"Frame size: {info.FrameSize}");
            Console.WriteLine($"Pixel format: {info.PixelFormat}");
            Console.WriteLine($"Codec: {info.CodecName}");
            Console.WriteLine($"Interlaced: {info.IsInterlaced}");
        }
        else
        {
            Close();
            return;
        }

        VertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(VertexArrayObject);

        VertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        ElementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        Shader.Use();

        var locationVertices = Shader.GetAttribLocation("vertices");
        GL.EnableVertexAttribArray(locationVertices);
        GL.VertexAttribPointer(locationVertices, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        //                                       ^ 3 vertex is 3 floats                   ^ 5 per row        ^ 0 offset per row

        var locationTexCoords = Shader.GetAttribLocation("vertexTexCoords");
        GL.EnableVertexAttribArray(locationTexCoords);
        GL.VertexAttribPointer(locationTexCoords, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        //                                        ^ tex coords is 2 floats                 ^ 5 per row        ^ 4th and 5th float in each row
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        Shader.Use();

        var currentTime = TimeSpan.FromSeconds(Clock.Elapsed.TotalSeconds);
        MP4.Update(currentTime);
        Shader.SetTexture("video", MP4.TextureHandle, TextureUnit.Texture0);

        GL.BindVertexArray(VertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();

        CalculateFPS();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        var input = KeyboardState;

        if (input.IsKeyReleased(Keys.Escape))
        {
            Close();
            Console.WriteLine($"\n\n{FramesPerSecond} FPS\n{AverageFramesPerSecond} average FPS, last {AverageFPSTimeframeSeconds} seconds");
            return;
        }

        if (input.IsKeyReleased(Keys.Space))
        {
            WindowState = (WindowState == WindowState.Fullscreen) ? WindowState.Normal : WindowState.Fullscreen;
        }
    }

    public new void Dispose()
    {
        base.Dispose();
        MP4.Dispose();
    }
}
