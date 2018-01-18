using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioReader
{
    class GlslRenderer : GameWindow
    {
        #region Structs

        struct Vec2i
        {
            public int X;
            public int Y;

            public Vec2i(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        class ShaderProgram
        {
            public int Id;
            public Dictionary<String, int> UniformLocations = new Dictionary<string, int>();
            public Dictionary<String, int> AttributeLocations = new Dictionary<string, int>();

            ~ShaderProgram()
            {
                GL.DeleteProgram(Id);
            }

            public void Reset()
            {
                GL.DeleteProgram(Id);
                UniformLocations.Clear();
                AttributeLocations.Clear();
            }

            public void Use()
            {
                GL.UseProgram(Id);
            }

            public void CacheUniformLocations(params string[] labels)
            {
                Log.Verbose("GLSL Renderer", "Caching uniforms for program " + Id + ".");
                foreach (string label in labels)
                {
                    UniformLocations[label] = GL.GetUniformLocation(Id, label);
                    Log.Verbose("GLSL Renderer", "Cached uniform " + label + " with value " + UniformLocations[label] + ".");
                }
            }

            public void CacheAttributeLocations(params string[] labels)
            {
                Log.Verbose("GLSL Renderer", "Caching attributes for program " + Id + ".");
                foreach (string label in labels)
                {
                    AttributeLocations[label] = GL.GetAttribLocation(Id, label);
                    Log.Verbose("GLSL Renderer", "Cached attribute " + label + " with value " + AttributeLocations[label] + ".");
                }
            }

            public int GetUniform(string label)
            {
                return UniformLocations.TryGetValue(label, out int position) ? position : -1;
            }

            public int GetAttribute(string label)
            {
                return AttributeLocations.TryGetValue(label, out int position) ? position : -1;
            }
        }

        #endregion Structs

        #region Member

        private uint _triangleArray;
        private uint _triangleBuffer;
        private ShaderProgram _currentProgram = new ShaderProgram();
        private DateTime _startTime = DateTime.Now;
        private double _time;
        private Vec2i _mouse = new Vec2i(0, 0);
        private Vec2i _resolution = new Vec2i(0, 0);

        #endregion Member

        #region Main

        public GlslRenderer() : base(800, 600, GraphicsMode.Default, "GLSL Renderer")
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Log.Info("GLSL Renderer", "Setting up renderer...");
            GL.ClearColor(Color4.Blue);

            _setupWindow();
            _compileShader("Shader/GlslSandbox/Fractal.frag");
            _setupVBO();
            Log.Info("GLSL Renderer", "Renderer setup complete.");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _setupWindow();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _render();
        }

        #endregion Main

        #region Helper

        private void _setupWindow()
        {
            int width = ClientRectangle.Width;
            int height = ClientRectangle.Height;
            GL.Viewport(0, 0, width, height);
            _resolution = new Vec2i(width, height);
            Log.Debug("GLSL Renderer", "Resized window.");
        }

        private void _setupVBO()
        {
            Log.Debug("GLSL Renderer", "Setting up VBO...");
            GL.GenVertexArrays(1, out _triangleArray);
            GL.BindVertexArray(_triangleArray);
            GL.GenBuffers(1, out _triangleBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _triangleBuffer);
            GL.EnableClientState(ArrayCap.VertexArray);
            int width = ClientRectangle.Width;
            int height = ClientRectangle.Height;
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 3 * sizeof(float), new float[] { -1f, -1f, 0f, 1f, -1f, 0f, 1f, 1f, 0f, -1f, 1f, 0f}, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(_currentProgram.GetAttribute("position"), 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(_currentProgram.GetAttribute("position"));
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Log.Debug("GLSL Renderer", "VBO setup complete.");
        }

        private void _compileShader(string fsPath)
        {
            Log.Debug("GLSL Renderer", "Compiling shaders...");
            int program = _createProgram("Shader/GlslSandboxFramework/ScreenShader.vert", fsPath);

            if (_currentProgram.Id > 0)
                _currentProgram.Reset();

            _currentProgram.Id = program;
            _currentProgram.Use();
            _currentProgram.CacheUniformLocations("time", "mouse", "resolution", "backbuffer", "surfaceSize");
            _currentProgram.CacheAttributeLocations("position");
            GL.EnableVertexAttribArray(_currentProgram.AttributeLocations["position"]);
            Log.Debug("GLSL Renderer", "Shader setup done.");
        }

        private int _createProgram(string vsPath, string fsPath)
        {
            int program;

            using (StreamReader vs = new StreamReader(vsPath))
            using (StreamReader fs = new StreamReader(fsPath))
            {
                int vertexObject = GL.CreateShader(ShaderType.VertexShader);
                int fragmentObject = GL.CreateShader(ShaderType.FragmentShader);

                // Compile vertex shader
                GL.ShaderSource(vertexObject, vs.ReadToEnd());
                GL.CompileShader(vertexObject);
                GL.GetShaderInfoLog(vertexObject, out string info);
                GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out int status_code);

                if (status_code != 1)
                    throw new ApplicationException(info);

                // Compile fragment shader
                GL.ShaderSource(fragmentObject, fs.ReadToEnd());
                GL.CompileShader(fragmentObject);
                GL.GetShaderInfoLog(fragmentObject, out info);
                GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);

                if (status_code != 1)
                    throw new ApplicationException(info);

                program = GL.CreateProgram();
                GL.AttachShader(program, vertexObject);
                GL.AttachShader(program, fragmentObject);
                GL.LinkProgram(program);
                GL.GetProgramInfoLog(program, out info);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status_code);

                if (status_code != 1)
                    throw new ApplicationException(info);

                GL.DetachShader(program, vertexObject);
                GL.DeleteShader(vertexObject);
                GL.DetachShader(program, fragmentObject);
                GL.DeleteShader(fragmentObject);
            }

            return program;
        }

        private void _render()
        {
            _time = (DateTime.Now - _startTime).TotalMilliseconds;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _currentProgram.Use();
            GL.Uniform1(_currentProgram.GetUniform("time"), (float)_time);
            GL.Uniform2(_currentProgram.GetUniform("mouse"), (float)_mouse.X, _mouse.Y);
            GL.Uniform2(_currentProgram.GetUniform("resolution"), (float)_resolution.X, _resolution.Y);
            GL.BindVertexArray(_triangleArray);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            SwapBuffers();
        }

        #endregion Helper
    }
}
