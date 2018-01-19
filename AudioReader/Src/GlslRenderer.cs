using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioReader
{
    class GlslRenderer : GameWindow
    {
        #region HelperClasses

        private delegate void SetupOutputHandler(object sender, EventArgs e);
        private delegate void RenderOutputHandler(object sender, EventArgs e);

        private struct Vec2<T>
        {
            public T X;
            public T Y;

            public Vec2(T x, T y)
            {
                X = x;
                Y = y;
            }
        }

        private class ShaderProgram
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

            public bool TryGetUniform(string label, out int location)
            {
                return UniformLocations.TryGetValue(label, out location);
            }

            public bool TryGetAttribute(string label, out int location)
            {
                return AttributeLocations.TryGetValue(label, out location);
            }
        }

        public class OutputTexture
        {
            private static ShaderProgram _program;
            private GlslRenderer _parent;
            private Vec2<int> _resolution;
            private Vec2<float> _originOffset;
            private Vec2<float> _originSize;
            private bool _useNearest;
            private uint _framebuffer;
            public int _texture;
            private byte[] _data;
            public bool Ready { get; private set; } = false;


            public OutputTexture(GlslRenderer parent, int resolutionX, int resolutionY, float originOffsetX = 0, float originOffsetY = 0, float originSizeX = 1, float originSizeY = 1, bool useNearest = true)
            {
                _parent = parent;
                _resolution = new Vec2<int>(resolutionX, resolutionY);
                _originOffset = new Vec2<float>(originOffsetX, originOffsetY);
                _originSize = new Vec2<float>(originSizeX, originSizeY);
                _useNearest = useNearest;
                _parent._setupOutput += _setup;
            }

            ~OutputTexture()
            {
                _parent._renderOutput -= _render;
                _parent._leftoverIds.Enqueue(new Tuple<uint, int>(_framebuffer, _texture));
            }

            private void _setup(object sender, EventArgs e)
            {
                _parent._setupOutput -= _setup;
                if (_program == null)
                    _setupProgram();
                _setupFramebuffer();
                _data = new byte[3 * _resolution.X * _resolution.Y];
                _parent._renderOutput += _render;
                Ready = true;
            }

            private void _setupProgram()
            {
                _program = new ShaderProgram
                {
                    Id = _parent._createProgram("Shader/GlslSandboxFramework/CopyPositionAttribute.vert", "Shader/GlslSandboxFramework/Resample.frag")
                };
                _program.Use();
                _program.CacheUniformLocations("originOffset", "originSize", "texture");
                _program.CacheAttributeLocations("position");

                GL.BindVertexArray(_parent._triangleArray);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _parent._triangleBuffer);
                GL.EnableClientState(ArrayCap.VertexArray);
                if (!_program.TryGetAttribute("position", out int position))
                    Log.Error("GLSL Renderer", "Vertex shader of resample program doesn't use position attribute.");
                GL.VertexAttribPointer(position, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(position);
                GL.DisableClientState(ArrayCap.VertexArray);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }

            private void _setupFramebuffer()
            {
                Log.Debug("OutputTexture", "Setting up framebuffer...");
                GL.GenFramebuffers(1, out _framebuffer);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
                GL.GenTextures(1, out _texture);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _resolution.X, _resolution.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _texture, 0);
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                {
                    Log.Error("OutputTexture", "Could not setup framebuffer: " + GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer));
                    return;
                }
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                Log.Debug("OutputTexture", "Framebuffer setup complete.");
            }

            private void _render(object sender, EventArgs e)
            {
                GL.ClearColor(Color4.Blue);
                _program.Use();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
                GL.Viewport(0, 0, _resolution.X, _resolution.Y);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                if (_program.TryGetUniform("originOffset", out int originOffset))
                    GL.Uniform2(originOffset, _originOffset.X, _originOffset.Y);
                if (_program.TryGetUniform("originSize", out int originSize))
                    GL.Uniform2(originSize, _originSize.X, _originSize.Y);

                GL.Enable(EnableCap.Texture2D);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _parent._texture);
                GL.GetTexParameter(TextureTarget.ProxyTexture2D, GetTextureParameter.TextureMinFilter, out int previousMin);
                GL.GetTexParameter(TextureTarget.ProxyTexture2D, GetTextureParameter.TextureMagFilter, out int previousMag);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)(_useNearest ? TextureMinFilter.Nearest : TextureMinFilter.Linear));
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)(_useNearest ? TextureMagFilter.Nearest : TextureMagFilter.Linear));
                if (_program.TryGetUniform("texture", out int texture))
                    GL.Uniform1(texture, 0);
                
                GL.BindVertexArray(_parent._triangleArray);
                GL.DrawArrays(PrimitiveType.Quads, 0, 4);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, previousMin);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, previousMag);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.UnsignedByte, _data);
            }

            public byte[] GetImage()
            {
                if(!Ready)
                    return new byte[0];
                byte[] image = new byte[_data.Length];
                _data.CopyTo(image, 0);
                return image;
            }
        }

        #endregion HelperClasses

        #region Member

        private event RenderOutputHandler _renderOutput;
        private event SetupOutputHandler _setupOutput;
        private ConcurrentQueue<Tuple<uint, int>> _leftoverIds = new ConcurrentQueue<Tuple<uint, int>>();
        private uint _triangleArray;
        private uint _triangleBuffer;
        private uint _framebuffer;
        private uint _texture;
        private ShaderProgram _textureProgram = new ShaderProgram();
        private ShaderProgram _screenProgram = new ShaderProgram();
        private DateTime _startTime = DateTime.Now;
        private double _time;
        private Vec2<int> _mouse = new Vec2<int>(0, 0);
        private Vec2<int> _resolution = new Vec2<int>(0, 0);
        private int _textureResolution;
        private float[] _audioData;
        private DateTime _lastBeat = DateTime.Now;
        private float _timeSinceLastBeat = 0f;

        #endregion Member

        #region Main

        public GlslRenderer(float[] audioData) : base(
            Config.GetDefault("glsl/window_width", 800), 
            Config.GetDefault("glsl/window_height", 600), 
            GraphicsMode.Default, 
            "GLSL Renderer")
        {
            _audioData = audioData;

            Mouse.Move += _mouseMove;
            Keyboard.KeyDown += _keyDown;

            _textureResolution = Config.GetDefault("glsl/resolution", 128);

            BeatDetection.BeatDetected += (object sender, EventArgs e) =>
            {
                _lastBeat = DateTime.Now;
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.ClearColor(Color4.Black);

            Log.Info("GLSL Renderer", "Setting up renderer...");

            _resizeWindow();
            _compileShaders("Shader/GlslSandbox/" + Config.GetDefault("glsl/shader", "Spectrum.frag"));
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
            _resolution = new Vec2<int>(ClientRectangle.Width, ClientRectangle.Height);
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
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 3 * sizeof(float), new float[] { -1f, -1f, 0f, 1f, -1f, 0f, 1f, 1f, 0f, -1f, 1f, 0f}, BufferUsageHint.StaticDraw);
            if(!_textureProgram.TryGetAttribute("position", out int texPosition))
                Log.Error("GLSL Renderer", "Vertex shader of texture program doesn't use position attribute.");
            GL.VertexAttribPointer(texPosition, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(texPosition);
            if (!_screenProgram.TryGetAttribute("position", out int scrPosition))
                Log.Error("GLSL Renderer", "Vertex shader of screen program doesn't use position attribute.");
            GL.VertexAttribPointer(scrPosition, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(scrPosition);
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
            _textureProgram.CacheUniformLocations("time", "mouse", "resolution", "audioData", "lastBeat");
            _textureProgram.CacheAttributeLocations("position");

            if (_screenProgram.Id > 0)
                _screenProgram.Reset();
            _screenProgram.Id = _createProgram("Shader/GlslSandboxFramework/CopyPositionAttribute.vert", "Shader/GlslSandboxFramework/ScreenShader.frag");
            _screenProgram.Use();
            _screenProgram.CacheUniformLocations("texture");
            _screenProgram.CacheAttributeLocations("position");

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
                GL.ShaderSource(vertexObject, _applyPlaceholder(vs.ReadToEnd()));
                GL.CompileShader(vertexObject);
                GL.GetShaderInfoLog(vertexObject, out string info);
                GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out int status_code);

                if (status_code != 1)
                    throw new ApplicationException(info);

                // Compile fragment shader
                GL.ShaderSource(fragmentObject, _applyPlaceholder(fs.ReadToEnd()));
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

        private string _applyPlaceholder(string shaderSource)
        {
            string result = shaderSource.Replace("%AUDIODATASIZE%", _audioData.Length.ToString());
            return result;
        }

        private void _render()
        {
            _time = (DateTime.Now - _startTime).TotalMilliseconds;
            _timeSinceLastBeat = (float)((DateTime.Now - _lastBeat).TotalMilliseconds);

            // set up or clean up output textures
            _setupOutput?.Invoke(this, EventArgs.Empty);
            if(_leftoverIds.TryDequeue(out Tuple<uint, int> leftoverIds))
            {
                GL.DeleteFramebuffer(leftoverIds.Item1);
                GL.DeleteTexture(leftoverIds.Item2);
            }

            // draw to texture
            _textureProgram.Use();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            GL.Viewport(0, 0, _textureResolution, _textureResolution);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            if (_textureProgram.TryGetUniform("time", out int texTime))
                GL.Uniform1(texTime, (float)_time);
            if (_textureProgram.TryGetUniform("mouse", out int texMouse))
                GL.Uniform2(texMouse, (float)_mouse.X / _resolution.X, (float)_mouse.Y / _resolution.Y);
            if (_textureProgram.TryGetUniform("resolution", out int texResolution))
                GL.Uniform2(texResolution, (float)_textureResolution, _textureResolution);
            if (_textureProgram.TryGetUniform("audioData", out int texAudioData))
                GL.Uniform1(texAudioData, _audioData.Length, _audioData);
            if (_textureProgram.TryGetUniform("lastBeat", out int texBeat))
                GL.Uniform1(texBeat, _timeSinceLastBeat);

            GL.BindVertexArray(_triangleArray);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            // draw to output textures
            _renderOutput?.Invoke(this, EventArgs.Empty);

            // draw to screen
            _screenProgram.Use();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, _resolution.X, _resolution.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.Texture2D);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            if (_screenProgram.TryGetUniform("texture", out int scrTexture))
                GL.Uniform1(scrTexture, 0);

            GL.BindVertexArray(_triangleArray);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            SwapBuffers();
        }

        #endregion Helper

        #region Events

        private void _mouseMove(object sender, MouseMoveEventArgs e)
        {
            _mouse.X = e.X;
            _mouse.Y = e.Y;
        }

        private void _keyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Exit();

            if (e.Key == Key.F11)
                if (WindowState == WindowState.Fullscreen)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Fullscreen;
        }

        #endregion Events
    }
}
