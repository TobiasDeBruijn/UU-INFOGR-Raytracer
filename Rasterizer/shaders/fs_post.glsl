#version 330

// shader inputs
in vec2 uv;						// fragment uv texture coordinates
in vec2 positionFromBottomLeft;
uniform sampler2D pixels;		// input texture (1st pass render target)

// shader output
out vec3 outputColor;

// fragment shader
void main()
{
	// retrieve input pixel
	outputColor = texture(pixels, uv).rgb;

	// apply dummy postprocessing effect
	float dist = length(positionFromBottomLeft);
	outputColor *= sin(dist * 50.0) * 0.25 + 0.75;
}