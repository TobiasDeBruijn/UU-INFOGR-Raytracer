using OpenTK.Graphics.OpenGL4;

namespace Template; 

public sealed class Shader : IDisposable {
    public int Handle { get; }
    private bool isDisposed = false;

    public Shader(string vertexPath, string fragPath) {
        int vertexHandle = CompileShader(vertexPath, ShaderType.VertexShader);
        int fragHandle = CompileShader(fragPath, ShaderType.FragmentShader);
        
        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexHandle);
        GL.AttachShader(Handle, fragHandle);
        GL.LinkProgram(Handle);
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0) {
            Console.Error.WriteLine(GL.GetProgramInfoLog(Handle));
        }
        
        GL.DetachShader(Handle, vertexHandle);
        GL.DetachShader(Handle, fragHandle);
        GL.DeleteShader(vertexHandle);
        GL.DeleteShader(fragHandle);
    }

    public void Use() {
        GL.UseProgram(Handle);
    }

    private static int CompileShader(string sourcePath, ShaderType shaderType) {
        string source = File.ReadAllText(sourcePath);
        int handle = GL.CreateShader(shaderType);
        GL.ShaderSource(handle, source);
        GL.CompileShader(handle);
        GL.GetShader(handle, ShaderParameter.CompileStatus, out int success);
        if (success == 0) {
            Console.Error.WriteLine(GL.GetShaderInfoLog(handle));
        }

        return handle;
    }

    private void Dispose(bool disposing) {
        if (!isDisposed) {
            GL.DeleteProgram(Handle);
            isDisposed = true;
        }
    }

    ~Shader() {
        if (!isDisposed) {
            Console.Error.WriteLine("GPU resource leak detected. Did you forget to call Dispose()?");
        }
    }

    public void Dispose() {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }    
}