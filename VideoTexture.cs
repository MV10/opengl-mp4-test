
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace opengl_mp4_test;

public class VideoTexture : IDisposable
{
    private TimeSpan _lastUpdateTime = TimeSpan.Zero;

    public MediaFile? File;
    public VideoStream? Stream;
    public int TextureHandle = -1;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsLoaded => File != null;
    public TimeSpan Duration => Stream?.Info.Duration ?? TimeSpan.Zero;

    public void Load(string filePath) => Load(filePath, new());

    public void Load(string filePath, MediaOptions options)
    {
        Dispose(); // Clean up if reloading

        // Require RGBA output for direct OpenGL compatibility
        options.VideoPixelFormat = ImagePixelFormat.Rgba32;

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

        if (currentTime > Duration)
        {
            currentTime = TimeSpan.Zero; // Loop for simplicity; adjust as needed
        }

        if (currentTime == _lastUpdateTime) return; // No need to update if time hasn't changed

        /*
        Performance Considerations: For sequential playback without seeking, consider switching to 
        TryGetNextFrame(out ImageData frame) which returns a boolean indicating success. The current 
        implementation uses GetFrame for flexible time-based updates, suitable for looping or non-linear playback.

        Error Handling: In production, add try-catch around GetFrame as it may throw exceptions on severe
        errors (e.g., corrupt files). The empty check handles most end-of-stream cases gracefully.
        */
        var frame = Stream.GetFrame(currentTime);

        if (!frame.Data.IsEmpty)
        {
            // Since we requested Rgba32, frame.PixelFormat should be Rgba32
            // Handle potential stride (padding) in the image data
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, frame.Stride / 4); // Stride in bytes, divide by bytes per pixel (4 for RGBA)

            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);

            // Flip the frame data vertically to match OpenGL's bottom-left origin (parallel is only faster for high-res videos)
            int bytesPerPixel = 4; // RGBA32
            int rowBytes = Width * bytesPerPixel;
            byte[] flippedData = new byte[frame.Data.Length];

            for (int y = 0; y < Height; y++)
            {
                // Copy row (Height - 1 - y) to row y
                int sourceOffset = y * frame.Stride;
                int destOffset = (Height - 1 - y) * rowBytes;
                frame.Data.Slice(sourceOffset, rowBytes).CopyTo(flippedData.AsSpan(destOffset, rowBytes));
            }

            // Update texture with new frame data
            unsafe
            {
                fixed (byte* ptr = flippedData)
                {
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                }
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Reset unpack row length to default
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);

            _lastUpdateTime = currentTime;
        }
    }

    /// <summary>
    /// Binds the texture for rendering (e.g., on a quad).
    /// </summary>
    public void Bind(int textureUnit = 0)
    {
        if (TextureHandle == -1) return;
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
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