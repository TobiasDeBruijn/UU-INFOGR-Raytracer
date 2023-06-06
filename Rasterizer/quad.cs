using OpenTK.Graphics.OpenGL;

namespace Rasterizer; 

public class ScreenQuad {
    // data members
    private int _vao;
    private int _vbo;

    private readonly float[] _vertices = { // x   y  z  u  v
        -1, 1, 0, 0, 1,
        1, 1, 0, 1, 1,
        -1, -1, 0, 0, 0,
        1, -1, 0, 1, 0
    };
    
    // initialization; called during first render
    private void Prepare(Shader shader) {
        if (_vbo != 0) {
            return;
        }
        
        _vao = GL.GenVertexArray();
        GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, _vao, -1, "VAO for ScreenQuad");
        GL.BindVertexArray(_vao);
        // prepare VBO for quad rendering
        GL.GenBuffers(1, out _vbo);
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, _vbo, -1, "VBO for ScreenQuad");
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(4 * 5 * 4), _vertices, BufferUsageHint.StaticDraw);
        // VBO contains vertices in correct order so no EBO needed
    }

    // render the mesh using the supplied shader and matrix
    public void Render(Shader shader, int textureId) {
        // on first run, prepare buffers
        Prepare(shader);

        // enable shader
        GL.UseProgram(shader.ProgramId);

        // enable texture
        int texLoc = GL.GetUniformLocation(shader.ProgramId, "pixels");
        GL.Uniform1(texLoc, 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureId);

        // enable position and uv attributes
        GL.EnableVertexAttribArray(shader.InVertexPositionObject);
        GL.EnableVertexAttribArray(shader.InVertexUv);

        // bind vertex data
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        // link vertex attributes to shader parameters 
        GL.VertexAttribPointer(shader.InVertexPositionObject, 3, VertexAttribPointerType.Float, false, 20, 0);
        GL.VertexAttribPointer(shader.InVertexUv, 2, VertexAttribPointerType.Float, false, 20, 3 * 4);

        // render (no EBO so use DrawArrays to process vertices in the order they're specified in the VBO)
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        // disable shader
        GL.UseProgram(0);
    }
}