# opengl-mp4-test

This wires up the basics of MP4 playback (without audio support) using my [eyecandy](https://github.com/MV10/eyecandy) library and OpenGL support via [OpenTK](https://github.com/opentk/opentk). FFMpeg support is provided by [FFMediaToolkit](https://github.com/radek-k/FFMediaToolkit). This has been successfully added to my [Monkey Hi Hat](https://github.com/MV10/monkey-hi-hat) music visualizer using code that is functionally identical to this example, so it should be a good starting point for anyone looking to add MP4 playback to an OpenGL / OpenTK app.

> It is necessary to download the ffmpeg binaries. The location of these files is specified in the `OnLoad` method of the `Win` class. I used the bin directory in `ffmpeg-7.1.1-full_build-shared.zip` from the [gyan.dev repo](https://github.com/GyanD/codexffmpeg/releases/tag/7.1.1), which is one of the sources recommended by the FFMediaToolkit README.

Note that eyecandy is used as a convenience here -- it provides some basic functionality from the OpenTK windowing support, shader compilation, and uniform handling, but it's entirely incidental to the video playback code. Also note that this example doesn't implement the thread-safety mutex synchronization mechanisms that eyecandy provides, because that only matters when eyecandy is doing audio processing on background threads, which is not used here.

The included `example.mp4` video is one of the small, low-resolution samples from the Monkey Hi Hat README. It is loaded in the `Win.OnLoad` method. Change that if you want to test with a different video.

Frankly I'm not super-impressed with the performance. My desktop is a fairly mid-range setup built in 2020, running an AMD Ryzen 9 3900XT (12 core, 3.8GHz), 16GB of RAM, and an NVIDIA GeForce RTX 2060. Running this app against the included example.mp4 video, I get the following perf metrics:

```
Average performance of first 10 non-skipped frames:
  Stream decoding: 1142.81 탎
  Frame inversion: 125.78 탎
  Texture copy: 46.93 탎
  Total frame time: 1321.98 탎
```

To be clear, this same machine can easily run Monkey Hi Hat shaders at hundreds or even thousands of frames per second, so 50 or 60 FPS when the shader is nothing but a pass-through is not great. With a more realistic video (such as Shadertoy's [in]famous [Britney Spears video](https://www.shadertoy.com/media/a/e81e818ac76a8983d746784b423178ee9f6cdcdf7f8e8d719341a6fe2d2ab303.webm), which is still a modest 512x396 25FPS 64kbps webm) the stream decoding overhead becomes more obvious. 

```
Average performance of first 10 non-skipped frames:
  Stream decoding: 1843.62 탎
  Frame inversion: 414.02 탎
  Texture copy: 558.12 탎
  Total frame time: 2832.42 탎
```

Stream decoding is just slow.

Given we're measuring microseconds, perf metrics will vary a LOT from one run to the next. If you want to try it on your system, run the app over and over again to get a sense of the range. Always use a Release build, and it's a lot more consistent on a "quiet" system running the app directly (VS still owns the console window and hooks outputs even in Release mode).

The maintainer of the FFMediaToolkit library has added an [internal flip option](https://github.com/radek-k/FFMediaToolkit/commit/fb7a3886e5e5b17b5186cde1ad30b6390c7da903), so the frame-inversion step _could_ be removed once that is released. I expected that to improve performance, but using a local build, to my surprise and great disappointment it was actually even slower... by a lot. Since this might vary on different hardware, for Monkey Hi Hat I implemented a switch so the user can select internal flipping, ffmpeg flipping, or even disable flipping entirely, which is useful for videos that are already oriented correctly. It looks like that feature will be released as v4.7.0 of FFMediaToolkit. You can see the option commented out at the top of the `VideoTexture.Load` method.

Eventually I may try to move video playback support into a background thread, as the CPU is generally under-utilized by Monkey Hi Hat. This should decouple video decoding from the render loop.
