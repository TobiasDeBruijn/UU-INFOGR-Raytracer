using System;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace Template
{
    public class Shader
    {
        // data members
        public int programID, vsID, fsID;
        public int in_vertexPositionObject;
        public int in_vertexNormalObject;
        public int in_vertexUV;
        public int uniform_objectToScreen;
        public int uniform_objectToWorld;

        // constructor
        public Shader(string vertexShader, string fragmentShader)
        {
            // compile shaders
            programID = GL.CreateProgram();
            GL.ObjectLabel(ObjectLabelIdentifier.Program, programID, -1, vertexShader + " + " + fragmentShader);
            Load(vertexShader, ShaderType.VertexShader, programID, out vsID);
            Load(fragmentShader, ShaderType.FragmentShader, programID, out fsID);
            GL.LinkProgram(programID);
            string infoLog = GL.GetProgramInfoLog(programID);
            if (infoLog.Length != 0) Console.WriteLine(infoLog);

            // get locations of shader parameters
            in_vertexPositionObject = GL.GetAttribLocation(programID, "vertexPositionObject");
            in_vertexNormalObject = GL.GetAttribLocation(programID, "vertexNormalObject");
            in_vertexUV = GL.GetAttribLocation(programID, "vertexUV");
            uniform_objectToScreen = GL.GetUniformLocation(programID, "objectToScreen");
            uniform_objectToWorld = GL.GetUniformLocation(programID, "objectToWorld");
        }

        // loading shaders
        void Load(String filename, ShaderType type, int program, out int ID)
        {
            // source: http://neokabuto.blogspot.nl/2013/03/opentk-tutorial-2-drawing-triangle.html
            ID = GL.CreateShader(type);
            GL.ObjectLabel(ObjectLabelIdentifier.Shader, ID, -1, filename);
            using (StreamReader sr = new StreamReader(filename)) GL.ShaderSource(ID, sr.ReadToEnd());
            GL.CompileShader(ID);
            GL.AttachShader(program, ID);
            string infoLog = GL.GetShaderInfoLog(ID);
            if (infoLog.Length != 0) Console.WriteLine(infoLog);
        }
    }
}
