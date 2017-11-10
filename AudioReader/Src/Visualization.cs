using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AudioReader
{
    public delegate void BeatEventHandler(object sender, EventArgs e);

    struct Vec2d
    {
        public double X;
        public double Y;

        public Vec2d(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

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

    class Parameters
    {
        private static Parameters _instance = null;
        public DateTime StartTime = DateTime.Now;
        public DateTime Time = DateTime.Now;
        public Vec2d Mouse = new Vec2d(0.5, 0.5);
        public Vec2d ScreenSize = new Vec2d(0, 0);

        private Parameters() { }

        public static Parameters GetInstance()
        {
            if (_instance == null)
                _instance = new Parameters();
            return _instance;
        }
    }

    class Surface
    {
        private static Surface _instance;
        public Vec2d Center = new Vec2d(0, 0);
        public Vec2d Size = new Vec2d(1, 1);
        public bool IsPanning = false;
        public bool IsZooming = false;
        public Vec2i Last = new Vec2i(0, 0);
        public Vec2i ClientLast = new Vec2i(0, 0);
        public int Buffer;

        private Surface() { }

        public static Surface GetInstance()
        {
            if (_instance == null)
                _instance = new Surface();
            return _instance;
        }
    }

    class Visualization : GameWindow
    {
        public event BeatEventHandler BeatDetected;

        private float[] _data;
        private int _samplesPerChannel = 128;
        private int _entriesPerChannel;
        private int _volumeSamples = 100;
        private Queue<double> _maxVolume;
        private int _bassSamples = 300;
        private Queue<double> _bassVolume;
        private int _localBassSamples = 10;
        private Queue<double> _localBassVolume;
        private bool _newBeat = true;
        private int _vertexShaderObject;
        private int _fragmentShaderObject;
        private int _shaderProgram;
        // for running GLSL Sandbox Shaders
        private Parameters _parameters = Parameters.GetInstance();
        private Surface _surface = Surface.GetInstance();
        private int _triangleBuffer;

        #region WindowManagement

        public Visualization(float[] data) : base(800, 600)
        {
            Keyboard.KeyDown += Keyboard_KeyDown;
            Mouse.ButtonDown += Mouse_ButtonDown;
            Mouse.ButtonUp += Mouse_ButtonUp;
            Mouse.Move += Mouse_Move;
            MouseLeave += Mouse_Leave;

            _data = data;
            _entriesPerChannel = _data.Length / 2;
            _maxVolume = new Queue<double>();
            _bassVolume = new Queue<double>();
            _localBassVolume = new Queue<double>();
        }

        protected virtual void OnBeatDetected(EventArgs e)
        {
            if (BeatDetected != null)
                BeatDetected(this, e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            using (StreamReader vs = new StreamReader("Shader/Simple/Simple.vert"))
            using (StreamReader fs = new StreamReader("Shader/Simple/Simple.frag"))
                CreateShaders(vs.ReadToEnd(), fs.ReadToEnd(), out _vertexShaderObject, out _fragmentShaderObject, out _shaderProgram);

            _setupWindow();

            GL.ClearColor(Color4.Black);
        }

        private void _setupWindow()
        {
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1, 1, 0, 1, -0.1, 1);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            _setupWindow();
        }

        #endregion WindowManagement

        #region OpenGLSetup

        void InitializeBuffers()
        {
            // Create vertex buffer (2 triangles)
            GL.CreateBuffers(1, out _triangleBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _triangleBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, 12, new double[] { -1.0, -1.0, 1.0, -1.0, -1.0, 1.0, 1.0, -1.0, 1.0, 1.0, -1.0, 1.0 }, BufferUsageHint.StaticDraw);
            // Create surface buffer (coordinates at screen corners)
            GL.CreateBuffers(1, out _surface.Buffer);
        }

        void CreateShaders(string vs, string fs, out int vertexObject, out int fragmentObject, out int program)
        {
            vertexObject = GL.CreateShader(ShaderType.VertexShader);
            fragmentObject = GL.CreateShader(ShaderType.FragmentShader);

            // Compile vertex shader
            GL.ShaderSource(vertexObject, vs);
            GL.CompileShader(vertexObject);
            GL.GetShaderInfoLog(vertexObject, out string info);
            GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out int status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // Compile fragment shader
            GL.ShaderSource(fragmentObject, fs);
            GL.CompileShader(fragmentObject);
            GL.GetShaderInfoLog(fragmentObject, out info);
            GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            program = GL.CreateProgram();
            GL.AttachShader(program, fragmentObject);
            GL.AttachShader(program, vertexObject);

            GL.LinkProgram(program);
            GL.UseProgram(program);
        }

        #endregion OpenGLSetup

        #region Helper

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static double MapClamped(double value, double oldMin, double oldMax, double newMin, double newMax)
        {
            if (oldMax - oldMin == 0) return newMax;
            double factor = Clamp((value - oldMin) / (oldMax - oldMin), 0, 1);
            return factor * newMax + (1 - factor) * newMin;
        }

        private double _getOffsettedValue(double index, int offset)
        {
            return _data[((int)index) * 2 + offset];
        }

        private double _interpolatedValue(double index, int offset)
        {
            double floor = Math.Floor(index);
            double ceiling = Math.Ceiling(index);
            if (floor == ceiling)
                return _getOffsettedValue(index, offset);
            return MapClamped(index, floor, ceiling, _getOffsettedValue(floor, offset), _getOffsettedValue(ceiling, offset));
        }

        #endregion Helper

        #region Keyboard

        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Exit();

            if (e.Key == Key.F11)
                if (this.WindowState == WindowState.Fullscreen)
                    this.WindowState = WindowState.Normal;
                else
                    this.WindowState = WindowState.Fullscreen;
        }

        #endregion Keyboard

        #region Mouse

        void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: enable once resetSurface is implemented
            //if (Keyboard.GetState().IsKeyDown(Key.LShift))
            //    resetSurface();
            if (e.Button == MouseButton.Left)
                _surface.IsPanning = true;
            else
                _surface.IsZooming = true;

            _surface.Last.X = e.X;
            _surface.Last.Y = e.Y;
        }

        void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
        {
            _surface.IsPanning = false;
            _surface.IsZooming = false;
        }

        void Mouse_Move(object sender, MouseMoveEventArgs e)
        {
            int clientX = e.X;
            int clientY = e.Y;

            if (_surface.ClientLast.X == clientX && _surface.ClientLast.Y == clientY)
                return;

            _surface.ClientLast.X = clientX;
            _surface.ClientLast.Y = clientY;

            int dx = clientX - _surface.Last.X;
            int dy = clientY - _surface.Last.Y;

            _parameters.Mouse.X = clientX / (double)Width;
            _parameters.Mouse.Y = clientY / (double)Height;

            if (_surface.IsPanning)
            {
                _surface.Center.X -= dx * _surface.Size.X / (double)Width;
                _surface.Center.Y -= dy * _surface.Size.Y / (double)Height;
            }
            else if (_surface.IsZooming)
            {
                _surface.Size.Y *= Math.Pow(0.997, dx + dy);
            }

            _surface.Last.X = clientX;
            _surface.Last.Y = clientY;
            // TODO: enable once computeSurfaceCorners is implemented
            //couputeSurfaceCorners();
        }

        void Mouse_Leave(object sender, EventArgs e)
        {
            _surface.IsPanning = false;
            _surface.IsZooming = false;
        }

        #endregion Mouse

        #region Rendering

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Color4(Color4.Gray);
            GL.LineWidth(2);

            // volume normalization
            double volumeMax = _maxVolume.Count > 0 ? _maxVolume.Max() : 0.01;
            double valueFactor = 0.8 / volumeMax;
            double maxVolume = 0;

            // bass detection
            double bassSum = 0;
            for (int i = 0; i < 10; i++) bassSum += _data[i];

            if (_bassVolume.Count >= _bassSamples)
                _bassVolume.Dequeue();
            _bassVolume.Enqueue(bassSum);

            if (_localBassVolume.Count >= _localBassSamples)
                _localBassVolume.Dequeue();
            _localBassVolume.Enqueue(bassSum);

            if (_localBassVolume.Average() > _bassVolume.Average() * 1.5)
            {
                if (_newBeat)
                {
                    OnBeatDetected(EventArgs.Empty);
                    _newBeat = false;
                }
                GL.Color4(Color4.White);
            }
            else
                _newBeat = true;

            // find base b so that b^(samples) = entries => logarithmic scaling on x-axis
            // b^(samples) = entries => b = samplest root of entries => b = entries^(1/samples)
            double b = Math.Pow(_entriesPerChannel, 1.0 / _samplesPerChannel);

            double[] sampleEdges = new double[_samplesPerChannel + 1];
            for (int i = 0; i <= _samplesPerChannel; i++)
                sampleEdges[i] = Math.Pow(b, i) - 1;

            GL.Begin(PrimitiveType.Lines);
            // max volume line
            GL.Vertex2(-1, volumeMax * valueFactor);
            GL.Vertex2(1, volumeMax * valueFactor);
            // center line
            GL.Vertex2(0, 0);
            GL.Vertex2(0, 1);
            GL.End();

            GL.LineWidth(5);
            GL.Begin(PrimitiveType.Lines);
            for (int i = 0; i < _samplesPerChannel; i++)
            {
                // sum up all values in range
                double left = 0;
                double right = 0;
                // the very last sample will be ignored, but those high frequencies rarely carry useful data
                double start = sampleEdges[i];
                double end = sampleEdges[i + 1];
                for (double j = start; j < end; j++)
                {
                    left += _interpolatedValue(j, 0);
                    right += _interpolatedValue(j, 1);
                }

                if (left > maxVolume) maxVolume = left;
                if (right > maxVolume) maxVolume = right;

                GL.Vertex2(-(double)i / _samplesPerChannel, left * valueFactor);
                GL.Vertex2(-(double)(i + 1) / _samplesPerChannel, left * valueFactor);

                GL.Vertex2((double)i / _samplesPerChannel, right * valueFactor);
                GL.Vertex2((double)(i + 1) / _samplesPerChannel, right * valueFactor);
            }
            GL.End();

            if (_maxVolume.Count >= _volumeSamples)
                _maxVolume.Dequeue();
            _maxVolume.Enqueue(maxVolume);

            SwapBuffers();
        }

        #endregion Rendering
    }
}
