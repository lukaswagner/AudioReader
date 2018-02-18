using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace AudioReader
{
    internal class OutputTexture
    {
        public bool Ready;
        public byte[] Data;
    }

    internal class GlslRenderer : GameWindow
    {
        #region HelperClasses

        private delegate void SetupTextureSetEvent(object sender, EventArgs e);

        private struct Vector2i
        {
            public int X;
            public int Y;

            public Vector2i(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static implicit operator Vector2(Vector2i vector2i) => new Vector2(vector2i.X, vector2i.Y);
        }

        private class Uniform
        {
            public string Name;
            private Type _type;
            private Func<object> _getter;

            public Uniform(string name, Type type, Func<object> getter)
            {
                Name = name;
                _type = type;
                _getter = getter;
            }

            public void Apply(ShaderProgram shaderProgram, object uniformValue = null)
            {
                if (shaderProgram.TryGetUniform(Name, out var position))
                {
                    var value = uniformValue ?? _getter.Invoke();
                    if (_type == typeof(float))
                        GL.Uniform1(position, (float)Convert.ChangeType(value, typeof(float)));
                    else if (_type == typeof(int))
                        GL.Uniform1(position, (int)Convert.ChangeType(value, typeof(int)));
                    else if (_type == typeof(Vector2))
                        GL.Uniform2(position, (Vector2)Convert.ChangeType(value, typeof(Vector2)));
                    else if (_type == typeof(Vector2i))
                        GL.Uniform2(position, (Vector2i)Convert.ChangeType(value, typeof(Vector2i)));
                    else if (_type == typeof(float[]))
                    {
                        var f = (float[])Convert.ChangeType(value, typeof(float[]));
                        GL.Uniform1(position, f.Length, f);
                    }
                    else if (_type == typeof(uint))
                    {
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, (uint)Convert.ChangeType(value, typeof(uint)));
                        GL.Uniform1(position, 0);
                    }
                    else
                        Log.Warn(_tag, "Could not apply uniform " + Name + " of type " + _type.Name);
                }
            }
        }

        internal class ShaderProgram
        {
            public int Id;
            public Dictionary<string, int> UniformLocations = new Dictionary<string, int>();
            public Dictionary<string, int> AttributeLocations = new Dictionary<string, int>();

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

            public void Use() => GL.UseProgram(Id);

            public void CacheUniformLocations(IEnumerable<string> labels)
            {
                Log.Verbose(_tag, "Caching uniforms for program " + Id + ".");
                foreach (var label in labels)
                {
                    UniformLocations[label] = GL.GetUniformLocation(Id, label);
                    Log.Verbose(_tag, "Cached uniform " + label + " with value " + UniformLocations[label] + ".");
                }
            }

            public void CacheUniformLocations(params string[] labels) => CacheUniformLocations((IEnumerable<string>)labels);

            public void CacheAttributeLocations(params string[] labels)
            {
                Log.Verbose(_tag, "Caching attributes for program " + Id + ".");
                foreach (var label in labels)
                {
                    AttributeLocations[label] = GL.GetAttribLocation(Id, label);
                    Log.Verbose(_tag, "Cached attribute " + label + " with value " + AttributeLocations[label] + ".");
                }
            }

            public bool TryGetUniform(string label, out int location) => UniformLocations.TryGetValue(label, out location);

            public bool TryGetAttribute(string label, out int location) => AttributeLocations.TryGetValue(label, out location);
        }

        private class Pipeline
        {
            private List<OutputTexture> Textures;

            private GlslRenderer _parent;
            private ShaderProgram[] _shaders;
            private List<TextureSet> _textureSets;

            public Pipeline(GlslRenderer renderer, string fsPath)
            {
                _parent = renderer;
                _setupShaders(new string[] { fsPath });
                _setupTextures();
            }

            private void _setupShaders(string[] fsPaths)
            {
                _shaders = new ShaderProgram[fsPaths.Length];
                for (var i = 0; i < fsPaths.Length; i++)
                {
                    _shaders[i] = new ShaderProgram
                    {
                        Id = _parent._createProgram("Shader/GlslSandboxFramework/CopyPositionAttribute.vert", fsPaths[i])
                    };
                    _shaders[i].Use();
                    _shaders[i].CacheUniformLocations(_parent._uniforms.Select((uniform) => uniform.Name));
                    _shaders[i].CacheAttributeLocations("position");

                    GL.BindVertexArray(_parent._triangleArray);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _parent._triangleBuffer);
                    GL.EnableClientState(ArrayCap.VertexArray);
                    if (!_shaders[i].TryGetAttribute("position", out var position))
                        Log.Error(_tag, "Vertex shader of shader program doesn't use position attribute.");
                    GL.VertexAttribPointer(position, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                    GL.EnableVertexAttribArray(position);
                    GL.DisableClientState(ArrayCap.VertexArray);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                }
            }

            private void _setupTextures()
            {
                _textureSets = new List<TextureSet>();
                Textures = new List<OutputTexture>();
                AddTextureSet(_parent._textureResolution, _parent._textureResolution);
            }

            public OutputTexture AddTextureSet(int resolutionX, int resolutionY, double offsetX = 0, double offsetY = 0, double sizeX = 1, double sizeY = 1)
            {
                Log.Debug(_tag, "resolutionX: " + resolutionX);
                Log.Debug(_tag, "resolutionY: " + resolutionY);
                _textureSets.Add(new TextureSet(resolutionX, resolutionY, _shaders.Length, offsetX, offsetY, sizeX, sizeY));
                _parent._setupTextureSets += _textureSets.Last().Setup;
                if (_textureSets.Count > 1)
                {
                    Textures.Add(new OutputTexture() { Ready = false, Data = new byte[resolutionX * resolutionY * 3] });
                    return Textures.Last();
                }
                return null;
            }

            public void Render()
            {
                for (var shaderIndex = 0; shaderIndex < _shaders.Length; shaderIndex++)
                {
                    var shader = _shaders[shaderIndex];
                    shader.Use();
                    foreach (var uniform in _parent._uniforms)
                        uniform.Apply(shader);
                    for (var textureIndex = 0; textureIndex < _textureSets.Count; textureIndex++)
                    {
                        var textureSet = _textureSets[textureIndex];
                        textureSet.ResolutionUniform.Apply(shader);

                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, textureSet.Buffers[shaderIndex]);
                        GL.Viewport(0, 0, textureSet.Resolution.X, textureSet.Resolution.Y);
                        GL.Clear(ClearBufferMask.ColorBufferBit);

                        GL.BindVertexArray(_parent._triangleArray);
                        GL.DrawArrays(PrimitiveType.Quads, 0, 4);

                        if (shaderIndex == _shaders.Length - 1 && textureIndex > 0)
                        {
                            Textures[textureIndex - 1].Data = textureSet.GetOutputBytes();
                            Textures[textureIndex - 1].Ready = true;
                        }
                    }
                }
            }

            public uint GetDisplayTexture() => _textureSets[0].Textures.Last();
        }

        private class TextureSet
        {
            public Vector2i Resolution { get; private set; }
            public int[] Buffers { get; private set; }
            public uint[] Textures { get; private set; }
            public Uniform ResolutionUniform;
            private Vector2 _offset;
            private Vector2 _size;
            public bool SetUp { get; private set; } = false;

            public TextureSet(int resolutionX, int resolutionY, int number, double offsetX, double offsetY, double sizeX, double sizeY)
            {
                Log.Debug(_tag, "Adding TextureSet with resolution " + resolutionX + "x" + resolutionY + ".");
                Resolution = new Vector2i(resolutionX, resolutionY);
                ResolutionUniform = new Uniform("resolution", typeof(Vector2i), () => Resolution);
                _offset = new Vector2((float)offsetX, (float)offsetY);
                _size = new Vector2((float)sizeX, (float)sizeY);
                Buffers = new int[number];
                Textures = new uint[number];
            }

            public void Setup(object sender, EventArgs e)
            {
                Log.Debug(_tag, "Setting up TextureSet with resolution " + Resolution.X + "x" + Resolution.Y + ".");
                ((GlslRenderer)sender)._setupTextureSets -= Setup;

                GL.GenFramebuffers(Buffers.Length, Buffers);
                GL.GenTextures(Textures.Length, Textures);

                foreach (var index in Enumerable.Range(0, Buffers.Length))
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, Buffers[index]);
                    GL.BindTexture(TextureTarget.Texture2D, Textures[index]);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Resolution.X, Resolution.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
                    GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, Textures[index], 0);
                    GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                    if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                    {
                        Log.Error("OutputTexture", "Could not setup framebuffer: " + GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer));
                        return;
                    }
                }
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }

            ~TextureSet()
            {
                GL.DeleteTextures(Buffers.Length, Buffers);
                GL.DeleteFramebuffers(Textures.Length, Textures);
            }

            public byte[] GetOutputBytes()
            {
                var data = new byte[Resolution.X * Resolution.Y * 3];
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.BindTexture(TextureTarget.Texture2D, Textures.Last());
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.UnsignedByte, data);
                return data;
            }
        }

        #endregion HelperClasses

        #region Static

        public static GlslRenderer Instance;
        private static Thread _renderThread;
        private static string _tag = "GLSL Renderer";

        public static void Enable(float[] reducedData)
        {
            _renderThread = new Thread(_threadFunction);
            _renderThread.Start(reducedData);
        }

        private static void _threadFunction(object reducedData)
        {
            Instance = new GlslRenderer((float[])reducedData);
            Instance.Run(60, Config.GetDefault("glsl/framerate", 60));
        }

        #endregion Static

        #region Member

        private event SetupTextureSetEvent _setupTextureSets;
        private uint _triangleArray;
        private uint _triangleBuffer;
        private ShaderProgram _screenProgram = new ShaderProgram();
        private DateTime _startTime = DateTime.Now;
        private double _time;
        private Vector2i _mouse = new Vector2i(0, 0);
        private Vector2i _resolution = new Vector2i(0, 0);
        private int _textureResolution;
        private float[] _audioData;
        private DateTime _lastBeat = DateTime.Now;
        private float _timeSinceLastBeat = 0f;
        private DateTime _lastMouse = DateTime.Now;
        private float _timeSinceLastMouse = 0f;
        private float _trackProgress = 0f;
        private bool _isPlaying = false;
        private byte[] _albumArtArray;
        private bool _albumArtUpdated;
        private uint _albumArt;
        private List<Uniform> _uniforms;
        private Pipeline _pipeline;

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

            Spotify.OnPlayingChanged += (bool isPlaying) => _isPlaying = isPlaying;
            Spotify.OnTimeChanged += (int time, int totalTime, double progress) => _trackProgress = (float)progress;
            Spotify.OnTrackChanged += (string title, string artist, string album, byte[] art) =>
            {
                _albumArtArray = art;
                _albumArtUpdated = true;
            };

            _isPlaying = Spotify.IsPlaying();
            _trackProgress = (float)Spotify.GetTrackProgressRatio();
            if(Spotify.GetAlbumArt(out var newArt))
            {
                _albumArtArray = newArt;
                _albumArtUpdated = true;
            }

            GL.GenTextures(1, out _albumArt);
            GL.BindTexture(TextureTarget.Texture2D, _albumArt);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.ClearColor(Color4.Black);

            Log.Info(_tag, "Setting up renderer...");

            _resizeWindow();
            _compileShaders("Shader/GlslSandbox/" + Config.GetDefault("glsl/shader", "Spectrum.frag"));
            _setupVBO();
            _uniforms = new List<Uniform>()
            {
                new Uniform("time", typeof(float), () => _time),
                new Uniform("mouse", typeof(Vector2), () => new Vector2((float)_mouse.X / _resolution.X, (float)_mouse.Y / _resolution.Y)),
                new Uniform("audioData", typeof(float[]), () => _audioData),
                new Uniform("lastBeat", typeof(float), () => _timeSinceLastBeat),
                new Uniform("lastMouse", typeof(float), () => _timeSinceLastMouse),
                new Uniform("trackProgress", typeof(float), () => _trackProgress),
                new Uniform("isPlaying", typeof(int), () => _isPlaying ? 1 : 0),
                new Uniform("albumArt", typeof(uint), () => _albumArt)
            };
            _pipeline = new Pipeline(this, "Shader/GlslSandbox/" + Config.GetDefault("glsl/shader", "Spectrum.frag"));

            Log.Info(_tag, "Renderer setup complete.");
            Program.RendererSetUp.Set();
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

        public OutputTexture RequestByteArray(int resolutionX, int resolutionY, double offsetX = 0, double offsetY = 0, double sizeX = 1, double sizeY = 1) => _pipeline.AddTextureSet(resolutionX, resolutionY, offsetX, offsetY, sizeX, sizeY);

        #endregion Main

        #region Helper

        private void _resizeWindow()
        {
            _resolution = new Vector2i(ClientRectangle.Width, ClientRectangle.Height);
            Log.Debug(_tag, "Resized window.");
        }

        private void _setupVBO()
        {
            Log.Debug(_tag, "Setting up VBO...");
            GL.GenVertexArrays(1, out _triangleArray);
            GL.BindVertexArray(_triangleArray);
            GL.GenBuffers(1, out _triangleBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _triangleBuffer);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 3 * sizeof(float), new float[] { -1f, -1f, 0f, 1f, -1f, 0f, 1f, 1f, 0f, -1f, 1f, 0f }, BufferUsageHint.StaticDraw);
            if (!_screenProgram.TryGetAttribute("position", out var scrPosition))
                Log.Error(_tag, "Vertex shader of screen program doesn't use position attribute.");
            GL.VertexAttribPointer(scrPosition, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(scrPosition);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Log.Debug(_tag, "VBO setup complete.");
        }

        private void _compileShaders(string fsPath)
        {
            Log.Debug(_tag, "Setting up shader programs...");

            if (_screenProgram.Id > 0)
                _screenProgram.Reset();
            _screenProgram.Id = _createProgram("Shader/GlslSandboxFramework/CopyPositionAttribute.vert", "Shader/GlslSandboxFramework/ScreenShader.frag");
            _screenProgram.Use();
            _screenProgram.CacheUniformLocations("texture");
            _screenProgram.CacheAttributeLocations("position");

            Log.Debug(_tag, "Shader setup done.");
        }

        private int _createProgram(string vsPath, string fsPath)
        {
            int program;

            using (var vs = new StreamReader(vsPath))
            using (var fs = new StreamReader(fsPath))
            {
                var vertexObject = GL.CreateShader(ShaderType.VertexShader);
                var fragmentObject = GL.CreateShader(ShaderType.FragmentShader);

                // Compile vertex shader
                GL.ShaderSource(vertexObject, _applyPlaceholder(vs.ReadToEnd()));
                GL.CompileShader(vertexObject);
                GL.GetShaderInfoLog(vertexObject, out var info);
                GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out var status_code);

                if (status_code != 1)
                    throw new GraphicsException(info);

                // Compile fragment shader
                GL.ShaderSource(fragmentObject, _applyPlaceholder(fs.ReadToEnd()));
                GL.CompileShader(fragmentObject);
                GL.GetShaderInfoLog(fragmentObject, out info);
                GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);

                if (status_code != 1)
                    throw new GraphicsException(info);

                program = GL.CreateProgram();
                GL.AttachShader(program, vertexObject);
                GL.AttachShader(program, fragmentObject);
                GL.LinkProgram(program);
                GL.GetProgramInfoLog(program, out info);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status_code);

                if (status_code != 1)
                    throw new GraphicsException(info);

                GL.DetachShader(program, vertexObject);
                GL.DeleteShader(vertexObject);
                GL.DetachShader(program, fragmentObject);
                GL.DeleteShader(fragmentObject);
            }

            return program;
        }

        private string _applyPlaceholder(string shaderSource)
        {
            var result = shaderSource.Replace("%AUDIODATASIZE%", _audioData.Length.ToString(CultureInfo.InvariantCulture));
            return result;
        }

        private void _render()
        {
            _time = (DateTime.Now - _startTime).TotalMilliseconds;
            _timeSinceLastBeat = (float)((DateTime.Now - _lastBeat).TotalMilliseconds);
            _timeSinceLastMouse = (float)((DateTime.Now - _lastMouse).TotalMilliseconds);
            if(_albumArtUpdated)
            {
                _albumArtUpdated = false;
                GL.BindTexture(TextureTarget.Texture2D, _albumArt);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 640, 640, 0, PixelFormat.Rgb, PixelType.UnsignedByte, _albumArtArray);
            }

            // set up or clean up output textures
            _setupTextureSets?.Invoke(this, EventArgs.Empty);

            // draw to output textures
            _pipeline.Render();

            // draw to screen
            _screenProgram.Use();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, _resolution.X, _resolution.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _pipeline.GetDisplayTexture());
            if (_screenProgram.TryGetUniform("texture", out var scrTexture))
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
            _lastMouse = DateTime.Now;
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

            if (e.Key == Key.Space)
                if (_isPlaying)
                    Spotify.Pause();
                else
                    Spotify.Play();
        }

        #endregion Events
    }
}
