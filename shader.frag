﻿#version 450
precision highp float;

in vec2 fragCoord;
uniform sampler2D video;
out vec4 fragColor;  
  
void main()
{
    vec3 texel = texture(video, fragCoord).rgb;
    fragColor = vec4(texel, 1.0);
}
