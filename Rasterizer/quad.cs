using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Template
{
    public class ScreenQuad
    {
        // data members
        int vao = 0, vbo = 0;
        float[] vertices =
        { // x   y  z  u  v
            -1,  1, 0, 0, 1,
             1,  1, 0, 1, 1,
            -1, -1, 0, 0, 0,
             1, -1, 0, 1, 0,
        };
        // constructor
        public ScreenQuad()
        {
        }

        // initialization; called during first render
        public void Prepare(Shader shader)
        {
            if (vbo == 0)
            {
                vao = GL.GenVertexArray();
                GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, vao, -1, "VAO for ScreenQuad");
                GL.BindVertexArray(vao);
                // prepare VBO for quad rendering
                GL.GenBuffers(1, out vbo);
                GL.ObjectLabel(ObjectLabelIdentifier.Buffer, vbo, -1, "VBO for ScreenQuad");
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(4 * 5 * 4), vertices, BufferUsageHint.StaticDraw);
                // VBO contains vertices in correct order so no EBO needed
            }
        }

        // render the mesh using the supplied shader and matrix
        public void Render(Shader shader, int textureID)
        {
            // on first run, prepare buffers
            Prepare(shader);

            // enable shader
            GL.UseProgram(shader.programID);

            // enable texture
            int texLoc = GL.GetUniformLocation(shader.programID, "pixels");
            GL.Uniform1(texLoc, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            // enable position and uv attributes
            GL.EnableVertexAttribArray(shader.in_vertexPositionObject);
            GL.EnableVertexAttribArray(shader.in_vertexUV);

            // bind vertex data
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            // link vertex attributes to shader parameters 
            GL.VertexAttribPointer(shader.in_vertexPositionObject, 3, VertexAttribPointerType.Float, false, 20, 0);
            GL.VertexAttribPointer(shader.in_vertexUV, 2, VertexAttribPointerType.Float, false, 20, 3 * 4);

            // render (no EBO so use DrawArrays to process vertices in the order they're specified in the VBO)
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            // disable shader
            GL.UseProgram(0);
        }
    }
}