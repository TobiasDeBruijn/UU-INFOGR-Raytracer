using OpenTK.Graphics.OpenGL;

namespace Rasterizer; 

public class Shader {
    // data members
    public readonly int ProgramId;
    public int VsId;
    public int FsId;
    public readonly int InVertexPositionObject;
    public readonly int InVertexNormalObject;
    public readonly int InVertexUv;
    public readonly int UniformObjectToScreen;
    public readonly int UniformObjectToWorld;

    // constructor
    public Shader(string vertexShader, string fragmentShader) {
        // compile shaders
        ProgramId = GL.CreateProgram();
        GL.ObjectLabel(ObjectLabelIdentifier.Program, ProgramId, -1, vertexShader + " + " + fragmentShader);
        Load(vertexShader, ShaderType.VertexShader, ProgramId, out VsId);
        Load(fragmentShader, ShaderType.FragmentShader, ProgramId, out FsId);
        GL.LinkProgram(ProgramId);
        string infoLog = GL.GetProgramInfoLog(ProgramId);
        if (infoLog.Length != 0) Console.WriteLine(infoLog);

        // get locations of shader parameters
        InVertexPositionObject = GL.GetAttribLocation(ProgramId, "vertexPositionObject");
        InVertexNormalObject = GL.GetAttribLocation(ProgramId, "vertexNormalObject");
        InVertexUv = GL.GetAttribLocation(ProgramId, "vertexUV");
        UniformObjectToScreen = GL.GetUniformLocation(ProgramId, "objectToScreen");
        UniformObjectToWorld = GL.GetUniformLocation(ProgramId, "objectToWorld");
    }

    // loading shaders
    private static void Load(string filename, ShaderType type, int program, out int id) {
        // source: http://neokabuto.blogspot.nl/2013/03/opentk-tutorial-2-drawing-triangle.html
        id = GL.CreateShader(type);
        GL.ObjectLabel(ObjectLabelIdentifier.Shader, id, -1, filename);
        using (var sr = new StreamReader(filename)) {
            GL.ShaderSource(id, sr.ReadToEnd());
        }

        GL.CompileShader(id);
        GL.AttachShader(program, id);
        string infoLog = GL.GetShaderInfoLog(id);
        if (infoLog.Length != 0) Console.WriteLine(infoLog);
    }
}