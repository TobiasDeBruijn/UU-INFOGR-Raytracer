// Only used in Modern OpenGL

#version 330

// shader inputs
in vec2 uv;			        // fragment uv texture coordinates
uniform sampler2D pixels;   // input texture (1st pass render target)

// shader output
out vec4 outputColor;

// fragment shader
void main()
{
    outputColor = texture(pixels, uv);
}