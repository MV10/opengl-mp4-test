# opengl-mp4-test

This wires up the basics of MP4 playback (without audio support) using my [eyecandy](https://github.com/MV10/eyecandy) library and OpenGL support via [OpenTK](https://github.com/opentk/opentk). FFMpeg support is provided by [FFMediaToolkit](https://github.com/radek-k/FFMediaToolkit). Eventually this will be added to my [Monkey Hi Hat](https://github.com/MV10/monkey-hi-hat) music visualizer.

It is necessary to download the ffmpeg binaries. The location of these files is specified in the `OnLoad` method of the `Win` class. I used the bin directory in `ffmpeg-7.1.1-full_build-shared.zip` from the [gyan.dev repo](https://github.com/GyanD/codexffmpeg/releases/tag/7.1.1), which is one of the sources recommended by the FFMediaToolkit README.

The included `example.mp4` video is one of the small, low-resolution samples from the Monkey Hi Hat README.

Frankly I'm not super-impressed with the performance. My desktop is a fairly mid-range setup built in 2020, running an AMD Ryzen 9 3900XT (12 core, 3.8GHz), 16GB of RAM, and an NVIDIA GeForce RTX 2060. Running this app against the included example.mp4 video, I get the following first-frame results:

```
55 average FPS, last 10 seconds
Skipped 360 of 618 buffer updates due to stream position not changing
First frame performance:
  Stream decoding: 3 탎
  Frame inversion: 278 탎
  Texture copy: 148 탎
  Total frame time: 574 탎
```

To be clear, this same machine can easily run Monkey Hi Hat shaders at hundreds or even thousands of frames per second, so 55 FPS when the shader is nothing but a pass-through is not great. While it looks like the frame inversion is the bottleneck, with a more realistic video (such as Shadertoy's [in]famous [Britney Spears video](https://www.shadertoy.com/media/a/e81e818ac76a8983d746784b423178ee9f6cdcdf7f8e8d719341a6fe2d2ab303.webm), which is still a modest 512x396 25FPS 64kbps webm) the stream decoding overhead becomes more obvious. 

```
81 average FPS, last 10 seconds
Skipped 702 of 939 buffer updates due to stream position not changing
First frame performance:
  Stream decoding: 777 탎
  Frame inversion: 449 탎
  Texture copy: 852 탎
  Total frame time: 141 탎
```

With some files I have tested with, stream decoding is more than 60% of the total frame time, which is not great.