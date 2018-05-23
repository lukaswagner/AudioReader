using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioReader
{
    internal static class GlOutput
    {
        private class OutputWindow : GameWindow
        {
            private OutputTexture _textureByteArray;
            private uint _triangleArray;
            private uint _triangleBuffer;
            private GlslRenderer.ShaderProgram _screenProgram = new GlslRenderer.ShaderProgram();
            private uint _texture;
            private int _width;
            private int _height;

            public OutputWindow(int width, int height, int zoom) : base(
                width * zoom,
                height * zoom,
                GraphicsMode.Default,
                "GL Output Test")
            {
                _width = width;
                _height = height;
                _textureByteArray = GlslRenderer.Instance.RequestByteArray(_width, _height);
                
                GL.GenTextures(1, out _texture);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _width, _height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
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
                
                _compileShaders();
                _setupVBO();

                Log.Info(_tag, "Renderer setup complete.");
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                base.OnRenderFrame(e);

                if (!_textureByteArray.Ready)
                    return;

                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, _textureByteArray.Data);

                _screenProgram.Use();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Viewport(0, 0, Width, Height);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                if (_screenProgram.TryGetUniform("texture", out var scrTexture))
                    GL.Uniform1(scrTexture, 0);

                GL.BindVertexArray(_triangleArray);
                GL.DrawArrays(PrimitiveType.Quads, 0, 4);

                SwapBuffers();
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

            private void _compileShaders()
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
                    GL.ShaderSource(vertexObject, vs.ReadToEnd());
                    GL.CompileShader(vertexObject);
                    GL.GetShaderInfoLog(vertexObject, out var info);
                    GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out var status_code);

                    if (status_code != 1)
                        throw new GraphicsException(info);

                    // Compile fragment shader
                    GL.ShaderSource(fragmentObject, fs.ReadToEnd());
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


        }

        private static string _tag = "OpenGL Output";

        public static void Enable()
        {
            var thread = new Thread(_outputLoop);
            thread.Start();
        }

        private static void _outputLoop()
        {
            var width = Config.GetDefault("gloutput/width", 100);
            var height = Config.GetDefault("gloutput/height", 100);
            var zoom = Config.GetDefault("gloutput/zoom", 10);
            
            var window = new OutputWindow(width, height, zoom);
            window.Run();
        }
    }
}
