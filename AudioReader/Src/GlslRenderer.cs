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

        #endregion Structs

        #region Member

        private uint _triangleArray;
        private uint _triangleBuffer;
        private int _currentProgram;
        private Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();
        private Dictionary<string, int> _attributeLocations = new Dictionary<string, int>();
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
            GL.ClearColor(Color4.Blue);

            _setupWindow();
            _compileShader("Shader/GlslSandbox/Fractal.frag");
            _setupVBO();
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
        }

        private void _setupVBO()
        {
            GL.GenVertexArrays(1, out _triangleArray);
            GL.BindVertexArray(_triangleArray);
            GL.GenBuffers(1, out _triangleBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _triangleBuffer);
            GL.EnableClientState(ArrayCap.VertexArray);
            int width = ClientRectangle.Width;
            int height = ClientRectangle.Height;
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 3 * sizeof(float), new float[] { -1f, -1f, 0f, 1f, -1f, 0f, 1f, 1f, 0f, -1f, 1f, 0f}, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(_attributeLocations["position"], 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(_attributeLocations["position"]);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void _compileShader(string fsPath)
        {
            int program = _createProgram("Shader/GlslSandboxFramework/ScreenShader.vert", fsPath);

            if (_currentProgram > 0)
                GL.DeleteProgram(_currentProgram);

            _currentProgram = program;
            GL.UseProgram(_currentProgram);
            _cacheUniformLocations(new string[] { "time", "mouse", "resolution", "backbuffer", "surfaceSize" });
            _cacheAttributeLocations(new string[] { "position" });
            GL.EnableVertexAttribArray(_attributeLocations["position"]);
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

        private void _cacheUniformLocations(string[] labels)
        {
            foreach (string label in labels)
            {
                _uniformLocations[label] = GL.GetUniformLocation(_currentProgram, label);
                Console.WriteLine("cached uniform " + label + " : " + _uniformLocations[label]);
            }
        }

        private void _cacheAttributeLocations(string[] labels)
        {
            foreach (string label in labels)
            {
                _attributeLocations[label] = GL.GetAttribLocation(_currentProgram, label);
                Console.WriteLine("cached attribute " + label + " : " + _attributeLocations[label]);
            }
        }

        private void _render()
        {
            _time = (DateTime.Now - _startTime).TotalMilliseconds;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            GL.UseProgram(_currentProgram);
            GL.Uniform1(_uniformLocations["time"], (float)_time);
            GL.Uniform2(_uniformLocations["mouse"], (float)_mouse.X, _mouse.Y);
            GL.Uniform2(_uniformLocations["resolution"], (float)_resolution.X, _resolution.Y);
            GL.BindVertexArray(_triangleArray);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            SwapBuffers();
        }

        #endregion Helper
    }
}
