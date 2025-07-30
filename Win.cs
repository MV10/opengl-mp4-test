
using eyecandy;
using FFMediaToolkit;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
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

        MP4.Load(@"C:\Source\GLMP4\GLMP4\bin\Debug\net8.0\example.mp4");
        if (!MP4.IsLoaded)
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
        MP4.Bind();
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
