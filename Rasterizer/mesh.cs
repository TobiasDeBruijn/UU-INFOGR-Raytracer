using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Rasterizer; 

// Mesh and MeshLoader based on work by JTalton; https://web.archive.org/web/20160123042419/www.opentk.com/node/642
// Only triangles and quads with vertex positions, normals, and texture coordinates are supported
public class Mesh {
    // data members
    private readonly string _filename; // for improved error reporting
    public ObjVertex[]? Vertices; // vertices (positions and normals in Object Space, and texture coordinates)
    public ObjTriangle[]? Triangles; // triangles (3 indices into the vertices array)
    private int _vertexBufferId; // vertex buffer object (VBO) for vertex data
    private int _triangleBufferId; // element buffer object (EBO) for triangle vertex indices

    // constructor
    public Mesh(string filename) {
        this._filename = filename;
        MeshLoader loader = new();
        loader.Load(this, filename);
    }

    // initialization; called during first render
    private void Prepare() {
        if (_vertexBufferId != 0) {
            return;
        }
        
        // generate interleaved vertex data array (uv/normal/position per vertex)
        GL.GenBuffers(1, out _vertexBufferId);
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, _vertexBufferId, 8 + _filename.Length, "VBO for " + _filename);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices?.Length * Marshal.SizeOf(typeof(ObjVertex))),
            Vertices, BufferUsageHint.StaticDraw);

        // generate triangle index array
        GL.GenBuffers(1, out _triangleBufferId);
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, _triangleBufferId, 17 + _filename.Length,
            "triangle EBO for " + _filename);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _triangleBufferId);
        GL.BufferData(BufferTarget.ElementArrayBuffer,
            (IntPtr)(Triangles?.Length * Marshal.SizeOf(typeof(ObjTriangle))), Triangles,
            BufferUsageHint.StaticDraw);
    }

    // render the mesh using the supplied shader and matrix
    public void Render(Shader shader, Matrix4 objectToScreen, Matrix4 objectToWorld, Texture texture) {
        // on first run, prepare buffers
        Prepare();

        // enable shader
        GL.UseProgram(shader.ProgramId);

        // enable texture
        int textureLocation =
            GL.GetUniformLocation(shader.ProgramId, "diffuseTexture"); // get the location of the shader variable
        const int textureUnit = 0; // choose a texture unit
        GL.Uniform1(textureLocation, textureUnit); // set the value of the shader variable to that texture unit
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit); // make that the active texture unit
        GL.BindTexture(TextureTarget.Texture2D,
            texture.Id); // bind the texture as a 2D image texture to the active texture unit

        // pass transforms to vertex shader
        GL.UniformMatrix4(shader.UniformObjectToScreen, false, ref objectToScreen);
        GL.UniformMatrix4(shader.UniformObjectToWorld, false, ref objectToWorld);

        // enable position, normal and uv attribute arrays corresponding to the shader "in" variables
        GL.EnableVertexAttribArray(shader.InVertexPositionObject);
        GL.EnableVertexAttribArray(shader.InVertexNormalObject);
        GL.EnableVertexAttribArray(shader.InVertexUv);

        // bind vertex data
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);

        // link vertex attributes to shader parameters 
        GL.VertexAttribPointer(shader.InVertexUv, 2, VertexAttribPointerType.Float, false, 32, 0);
        GL.VertexAttribPointer(shader.InVertexNormalObject, 3, VertexAttribPointerType.Float, true, 32, 2 * 4);
        GL.VertexAttribPointer(shader.InVertexPositionObject, 3, VertexAttribPointerType.Float, false, 32, 5 * 4);

        // bind triangle index data and render
        if (Triangles != null && Triangles.Length > 0) {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _triangleBufferId);
            GL.DrawArrays(PrimitiveType.Triangles, 0, Triangles.Length * 3);
        }

        // restore previous OpenGL state
        GL.UseProgram(0);
    }

    // layout of a single vertex
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjVertex {
        public Vector2 TexCoord;
        public Vector3 Normal;
        public Vector3 Vertex;
    }

    // layout of a single triangle
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjTriangle {
        public int Index0, Index1, Index2;
    }

    // layout of a single quad
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjQuad {
        public int Index0, Index1, Index2, Index3;
    }
}