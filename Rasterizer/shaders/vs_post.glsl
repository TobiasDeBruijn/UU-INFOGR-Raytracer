#version 330

// shader inputs
in vec3 vertexPositionObject;		// vertex position in Object Space
									// this shader assumes Object Space is identical to Screen Space
in vec2 vertexUV;					// vertex uv texture coordinates

// shader output, will be interpolated from vertices to fragments
out vec2 uv;						// vertex uv texture coordinates (pass-through)
out vec2 positionFromBottomLeft;	// vertex position on the screen, with (0, 0) at the bottom left and (1, 1) at the top right

// vertex shader
void main()
{
	// vertex position already in Screen Space so no transformation needed
	gl_Position = vec4(vertexPositionObject, 1.0);

	// pass the uv coordinate
	uv = vertexUV;

	positionFromBottomLeft = 0.5 * vertexPositionObject.xy + 0.5;
}