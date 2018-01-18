using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
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
        private uint _framebuffer;
        private uint _texture;
        private ShaderProgram _textureProgram = new ShaderProgram();
        private ShaderProgram _screenProgram = new ShaderProgram();
        private DateTime _startTime = DateTime.Now;
        private double _time;
        private Vec2i _mouse = new Vec2i(0, 0);
        private Vec2i _resolution = new Vec2i(0, 0);
        private int _textureResolution = 128;

        #endregion Member

        #region Main

        public GlslRenderer() : base(800, 600, GraphicsMode.Default, "GLSL Renderer")
        {
            Mouse.Move += _mouseMove;

            if (Config.Get("glsl/resolution", out string resolution))
                _textureResolution = Int32.Parse(resolution);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Log.Info("GLSL Renderer", "Setting up renderer...");
            GL.ClearColor(Color4.Blue);

            _resizeWindow();
            _compileShaders("Shader/GlslSandbox/Fractal.frag");
            _setupVBO();
            _setupFramebuffer();
            Log.Info("GLSL Renderer", "Renderer setup complete.");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _resizeWindow();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _render();
        }

        #endregion Main

        #region Helper

        private void _resizeWindow()
        {
            _resolution = new Vec2i(ClientRectangle.Width, ClientRectangle.Height);
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
            GL.VertexAttribPointer(_textureProgram.GetAttribute("position"), 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(_textureProgram.GetAttribute("position"));
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Log.Debug("GLSL Renderer", "VBO setup complete.");
        }

        private void _setupFramebuffer()
        {
            Log.Debug("GLSL Renderer", "Setting up framebuffer...");
            GL.GenFramebuffers(1, out _framebuffer);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            GL.GenTextures(1, out _texture);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _textureResolution, _textureResolution, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _texture, 0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Log.Error("GLSL Renderer", "Could not setup framebuffer: " + GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer));
                return;
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Log.Debug("GLSL Renderer", "Framebuffer setup complete.");
        }

        private void _compileShaders(string fsPath)
        {
            Log.Debug("GLSL Renderer", "Setting up shader programs...");

            if (_textureProgram.Id > 0)
                _textureProgram.Reset();
            _textureProgram.Id = _createProgram("Shader/GlslSandboxFramework/CopyPositionAttribute.vert", fsPath);
            _textureProgram.Use();
            _textureProgram.CacheUniformLocations("time", "mouse", "resolution", "backbuffer", "surfaceSize");
            _textureProgram.CacheAttributeLocations("position");
            GL.EnableVertexAttribArray(_textureProgram.AttributeLocations["position"]);

            if (_screenProgram.Id > 0)
                _screenProgram.Reset();
            _screenProgram.Id = _createProgram("Shader/GlslSandboxFramework/CopyPositionAttribute.vert", "Shader/GlslSandboxFramework/ScreenShader.frag");
            _screenProgram.Use();
            _screenProgram.CacheUniformLocations("texture");
            _screenProgram.CacheAttributeLocations("position");
            GL.EnableVertexAttribArray(_screenProgram.AttributeLocations["position"]);

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

            // draw to texture
            _textureProgram.Use();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            GL.Viewport(0, 0, _textureResolution, _textureResolution);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Uniform1(_textureProgram.GetUniform("time"), (float)_time);
            GL.Uniform2(_textureProgram.GetUniform("mouse"), (float)_mouse.X / _resolution.X, (float)_mouse.Y / _resolution.Y);
            GL.Uniform2(_textureProgram.GetUniform("resolution"), (float)_textureResolution, _textureResolution);

            GL.BindVertexArray(_triangleArray);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            // draw to screen
            _screenProgram.Use();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, _resolution.X, _resolution.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.Texture2D);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.Uniform1(_screenProgram.GetUniform("texture"), 0);

            GL.BindVertexArray(_triangleArray);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            SwapBuffers();
        }

        #endregion Helper

        #region Events

        void _mouseMove(object sender, MouseMoveEventArgs e)
        {
            _mouse.X = e.X;
            _mouse.Y = e.Y;
        }

            #endregion Events
        }
}
