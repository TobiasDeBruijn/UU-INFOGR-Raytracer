using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

// The template provides you with a window which displays a 'linear frame buffer', i.e.
// a 1D array of pixels that represents the graphical contents of the window.

// Under the hood, this array is encapsulated in a 'Surface' object, and copied once per
// frame to an OpenGL texture, which is then used to texture 2 triangles that exactly
// cover the window. This is all handled automatically by the template code.

// Before drawing the two triangles, the template calls the Tick method in MyApplication,
// in which you are expected to modify the contents of the linear frame buffer.

// After (or instead of) rendering the triangles you can add your own OpenGL code.

// We will use both the pure pixel rendering as well as straight OpenGL code in the
// tutorial. After the tutorial you can throw away this template code, or modify it at
// will, or maybe it simply suits your needs.

namespace Template
{
    public class OpenTkApp : GameWindow
    {
        /**
         * IMPORTANT:
         * 
         * Modern OpenGL (introduced in 2009) does NOT allow Immediate Mode or
         * Fixed-Function Pipeline commands, e.g., GL.MatrixMode, GL.Begin,
         * GL.End, GL.Vertex, GL.TexCoord, or GL.Enable certain capabilities
         * related to the Fixed-Function Pipeline. It also REQUIRES you to use
         * shaders.
         * 
         * If you want to try prehistoric OpenGL code, such as many code
         * samples still found online, enable it below.
         * 
         * MacOS doesn't support prehistoric OpenGL anymore since 2018.
         */
        public const bool AllowPrehistoricOpenGl = false;

        int _screenId;            // unique integer identifier of the OpenGL texture
        MyApplication? _app;      // instance of the application
        bool _terminated = false; // application terminates gracefully when this is true

        // The following variables are only needed in Modern OpenGL
        public int vertexArrayObject;
        public int vertexBufferObject;
        public int programId;
        // All the data for the vertices interleaved in one array:
        // - XYZ in normalized device coordinates
        // - UV
        readonly float[] _vertices =
        { //  X      Y     Z     U     V
            -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, // bottom-left  2-----3 triangles:
             1.0f, -1.0f, 0.0f, 1.0f, 1.0f, // bottom-right | \   |     012
            -1.0f,  1.0f, 0.0f, 0.0f, 0.0f, // top-left     |   \ |     123
             1.0f,  1.0f, 0.0f, 1.0f, 0.0f, // top-right    0-----1
        };

        public OpenTkApp()
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = new Vector2i(1280, 720),
                Profile = AllowPrehistoricOpenGl ? ContextProfile.Compatability : ContextProfile.Core,
                Flags = AllowPrehistoricOpenGl ? ContextFlags.Default : ContextFlags.ForwardCompatible,
            })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            CursorGrabbed = true;
            // called during application initialization
            GL.ClearColor(0, 0, 0, 0);
            GL.Disable(EnableCap.DepthTest);
            Surface screen = new(ClientSize.X, ClientSize.Y);
            _app = new MyApplication(screen);
            _screenId = _app.screen.GenTexture();
            if (AllowPrehistoricOpenGl)
            {
                GL.Enable(EnableCap.Texture2D);
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            }
            else
            {   // setting up a Modern OpenGL pipeline takes a lot of code
                // Vertex Array Object: will store the meaning of the data in the buffer
                vertexArrayObject = GL.GenVertexArray();
                GL.BindVertexArray(vertexArrayObject);
                // Vertex Buffer Object: a buffer of raw data
                vertexBufferObject = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
                // Vertex Shader
                string shaderSource = File.ReadAllText("../../../shaders/screen_vs.glsl");
                int vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, shaderSource);
                GL.CompileShader(vertexShader);
                GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int status);
                if (status != (int)All.True)
                {
                    string log = GL.GetShaderInfoLog(vertexShader);
                    throw new Exception($"Error occurred whilst compiling vertex shader ({vertexShader}):\n{log}");
                }
                // Fragment Shader
                shaderSource = File.ReadAllText("../../../shaders/screen_fs.glsl");
                int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, shaderSource);
                GL.CompileShader(fragmentShader);
                GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
                if (status != (int)All.True)
                {
                    string log = GL.GetShaderInfoLog(fragmentShader);
                    throw new Exception($"Error occurred whilst compiling fragment shader ({fragmentShader}):\n{log}");
                }
                // Program: a set of shaders to be used together in a pipeline
                programId = GL.CreateProgram();
                GL.AttachShader(programId, vertexShader);
                GL.AttachShader(programId, fragmentShader);
                GL.LinkProgram(programId);
                GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out status);
                if (status != (int)All.True)
                {
                    string log = GL.GetProgramInfoLog(programId);
                    throw new Exception($"Error occurred whilst linking program ({programId}):\n{log}");
                }
                // the program contains the compiled shaders, we can delete the source
                GL.DetachShader(programId, vertexShader);
                GL.DetachShader(programId, fragmentShader);
                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);
                // send all the following draw calls through this pipeline
                GL.UseProgram(programId);
                // tell the VAO which part of the VBO data should go to each shader input
                int location = GL.GetAttribLocation(programId, "vPosition");
                GL.EnableVertexAttribArray(location);
                GL.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
                location = GL.GetAttribLocation(programId, "vUV");
                GL.EnableVertexAttribArray(location);
                GL.VertexAttribPointer(location, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
                // connect the texture to the shader uniform variable
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _screenId);
                GL.Uniform1(GL.GetUniformLocation(programId, "pixels"), 0);
            }
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            // called upon app close
            GL.DeleteTextures(1, ref _screenId);
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            // called upon window resize. Note: does not change the size of the pixel buffer.
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            
            if (AllowPrehistoricOpenGl)
            {
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
            }
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            // called once per frame; app logic
            var keyboard = KeyboardState;
            if (keyboard[Keys.Escape]) _terminated = true;
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            // called once per frame; render
            if (_app != null) _app.Tick();
            if (_terminated)
            {
                Close();
                return;
            }
            // convert MyApplication.screen to OpenGL texture
            if (_app != null)
            {
                GL.BindTexture(TextureTarget.Texture2D, _screenId);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                               _app.screen.width, _app.screen.height, 0,
                               PixelFormat.Bgra,
                               PixelType.UnsignedByte, _app.screen.pixels
                             );
                // draw screen filling quad
                if (AllowPrehistoricOpenGl)
                {
                    GL.Begin(PrimitiveType.Quads);
                    GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(-1.0f, -1.0f);
                    GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(1.0f, -1.0f);
                    GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(1.0f, 1.0f);
                    GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-1.0f, 1.0f);
                    GL.End();
                }
                else
                {
                    GL.BindVertexArray(vertexArrayObject);
                    GL.UseProgram(programId);
                    GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
                }
            }
            // tell OpenTK we're done rendering
            SwapBuffers();
        }
        public static void Main()
        {
            // entry point
            using OpenTkApp app = new();
            app.RenderFrequency = 30.0;
            app.Run();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e) {
            base.OnKeyDown(e);
            _app!.OnKeyPress(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e) {
            base.OnMouseMove(e);
            _app!.OnMouseMove(e);
        }
    }
}