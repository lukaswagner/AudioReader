using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioReader
{
    class Visualization : GameWindow
    {
        private float[] _data;
        private int _samplesPerChannel = 128;
        private int _entriesPerChannel;
        private int _volumeSamples = 100;
        private Queue<double> _maxVolume;
        private int _bassSamples = 10000;
        private Queue<double> _bassVolume;

        #region WindowManagement

        public Visualization(float[] data) : base(2000, 1000)
        {
            _data = data;
            _entriesPerChannel = _data.Length / 2;
            _maxVolume = new Queue<double>();
            _bassVolume = new Queue<double>();
        }

        private void _setupWindow()
        {
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1, 1, 0, 1, -0.1, 1);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _setupWindow();

            GL.ClearColor(Color4.Black);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            _setupWindow();
        }

        #endregion WindowManagement

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
            if (bassSum > _bassVolume.Average() * 1.5) GL.Color4(Color4.White);

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
