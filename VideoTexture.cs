
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace opengl_mp4_test;

public class VideoTexture : IDisposable
{
    private TimeSpan _lastUpdateTime = TimeSpan.Zero;
    private TimeSpan _lastStreamPosition = TimeSpan.Zero;
    private long _frameCount = 0;
    private long _streamSkips = 0;

    private Stopwatch FrameClock = new();
    private Stopwatch PerfClock = new();

    // The first 10 decoded frames are used to produce average perf stats.
    private int _perfIndex = -1;
    public TimeSpan DecodePerf = TimeSpan.Zero;
    public TimeSpan InvertFramePerf = TimeSpan.Zero;
    public TimeSpan CopyTexturePerf = TimeSpan.Zero;
    public TimeSpan TotalFramePerf = TimeSpan.Zero;

    public MediaFile? File;
    public VideoStream? Stream;
    public int TextureHandle = -1;
    public int Width;
    public int Height;
    public bool IsLoaded => File != null;
    public TimeSpan Duration => Stream?.Info.Duration ?? TimeSpan.Zero;

    public void Load(string filePath) => Load(filePath, new());

    public void Load(string filePath, MediaOptions options)
    {
        // Require RGBA output for direct OpenGL compatibility
        options.VideoPixelFormat = ImagePixelFormat.Rgba32;

        // ******************************************************************************
        // added to v4.7.0 of FFMediaToolkit but this is slower than doing it locally
        //   options.FlipVertically = true; // OpenGL expects the origin is at the bottom-left
        // ******************************************************************************

        try
        {
            File = MediaFile.Open(filePath, options); 
            Stream = File.Video;

            if (Stream == null)
            {
                throw new InvalidOperationException("No video stream found in the file.");
            }

            Width = Stream.Info.FrameSize.Width;
            Height = Stream.Info.FrameSize.Height;

            // Create OpenGL texture
            TextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // Allocate texture storage (initially empty)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading video:\n{ex.Message}\n{ex.InnerException?.Message}");
            Dispose();
        }
    }

    /// <summary>
    /// Updates the texture with the frame at the specified time.
    /// Call this each render frame, passing elapsed time (e.g., from a game loop).
    /// </summary>
    /// <param name="currentTime">The desired playback time.</param>
    public void Update(TimeSpan currentTime)
    {
        if (!IsLoaded || Stream == null) return;

        if (currentTime > Duration) currentTime = TimeSpan.Zero; // Loop for simplicity; adjust as needed
        if (currentTime == _lastUpdateTime) return; // No need to update if time hasn't changed

        _frameCount++;
        if (_perfIndex < 10) _perfIndex++;

        if (_perfIndex == 10)
        {
            Console.WriteLine($"\nSkipped {_streamSkips} of {_frameCount} buffer updates due to stream position not changing");
            Console.WriteLine("Average performance of first 10 non-skipped frames:");
            Console.WriteLine($"  Stream decoding: {DecodePerf.TotalMicroseconds / 10:#.##} µs");
            Console.WriteLine($"  Frame inversion: {InvertFramePerf.TotalMicroseconds / 10:#.##} µs");
            Console.WriteLine($"  Texture copy: {CopyTexturePerf.TotalMicroseconds / 10:#.##} µs");
            Console.WriteLine($"  Total frame time: {TotalFramePerf.TotalMicroseconds / 10:#.##} µs");
            _perfIndex++; // Increment to avoid printing this again and to end perf collection
        }

        if (_perfIndex < 10)
        {
            FrameClock.Restart();
            PerfClock.Restart();
        }

        // The ffmpeg frame decoding is the most expensive operation by a large margin (90% of the frame time at 1080p).
        var frame = Stream.GetFrame(currentTime);

        if (_perfIndex < 10) PerfClock.Stop();

        bool skip = (frame.Data.IsEmpty || Stream.Position == _lastStreamPosition);
        if (skip)
        {
            _streamSkips++;
            if (_perfIndex < 10) _perfIndex--;
            FrameClock.Reset();
            PerfClock.Reset();
            return;
        }

        DecodePerf += PerfClock.Elapsed;

        if (!skip)
        {
            if (_perfIndex < 10) PerfClock.Restart();

            // Flip the frame data vertically to match OpenGL's bottom-left origin
            int rowBytes = Width * 4; // RGBA32 is 4 bpp
            byte[] flippedData = new byte[frame.Data.Length];
            for (int y = 0; y < Height; y++)
            {
                int sourceOffset = y * frame.Stride;
                int destOffset = (Height - 1 - y) * rowBytes;
                frame.Data.Slice(sourceOffset, rowBytes).CopyTo(flippedData.AsSpan(destOffset, rowBytes));
            }

            // Parallel version is about 40X slower for a 360p video, likely due to both thread-scheduling overhead and the
            // extra copy of the frame data needed (because the lambda can't access the frame.Data ref struct directly).
            // For 1080p video, the parallel version is about 7X slower, so parallel processing just has too much overhead.
            //var stride = frame.Stride;
            //var frameData = frame.Data.ToArray();
            //Parallel.For(0, Height, y =>
            //{
            //    int sourceOffset = y * stride;
            //    int destOffset = (Height - 1 - y) * rowBytes;
            //    Array.Copy(frameData, sourceOffset, flippedData, destOffset, rowBytes);
            //});

            if (_perfIndex < 10)
            {
                InvertFramePerf += PerfClock.Elapsed;
                PerfClock.Restart();
            }

            // The PixelStore calls are apparently not needed and eliminating them slightly improves performance.
            //GL.PixelStore(PixelStoreParameter.UnpackRowLength, frame.Stride / 4); // stride (padding) in bytes divided by 4 RGBA bytes per pixel
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
            unsafe
            {
                //fixed (byte* ptr = frame.Data)
                fixed (byte* ptr = flippedData)
                {
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                }
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
            //GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);

            if (_perfIndex < 10)
            {
                PerfClock.Stop();
                CopyTexturePerf += PerfClock.Elapsed;  
            }

            _lastUpdateTime = currentTime;
            _lastStreamPosition = Stream.Position;

            if (_perfIndex < 10) TotalFramePerf += FrameClock.Elapsed;
        }

        FrameClock.Reset();
    }

    public void Dispose()
    {
        if (TextureHandle != -1)
        {
            GL.DeleteTexture(TextureHandle);
            TextureHandle = -1;
        }

        File?.Dispose();
        File = null;
        Stream = null;
    }
}