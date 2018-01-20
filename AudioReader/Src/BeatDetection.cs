using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioReader
{
    internal delegate void BeatEventHandler(object sender, EventArgs e);
    internal static class BeatDetection
    {
        public static event BeatEventHandler BeatDetected;

        private static float[] _data;
        private static int _targetFramerate = Config.GetDefault("beatdetection/framerate", 60);
        private static double _targetFrametime = 1000d / _targetFramerate;
        private static DateTime _loopTime = DateTime.Now;
        private static int _bassSamples = _targetFramerate * 5;
        private static Queue<double> _bassVolume = new Queue<double>();
        private static int _localBassSamples = _targetFramerate / 6;
        private static Queue<double> _localBassVolume = new Queue<double>();
        private static bool _newBeat = true;
        private static Thread _beatDetectionLoopThread = new Thread(_beatDetectionLoop);
        private static bool _run = false;

        public static void Enable(float[] data)
        {
            _data = data;
            _run = true;
            _beatDetectionLoopThread.Start();
        }

        private static void _beatDetectionLoop()
        {
            while (_run)
            {
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
                        BeatDetected?.Invoke(new Object(), EventArgs.Empty);
                        _newBeat = false;
                        Log.Verbose("BeatDetection", "Beat detected.");
                    }
                }
                else
                    _newBeat = true;

                double loopTime = (DateTime.Now - _loopTime).TotalMilliseconds;
                Thread.Sleep(Math.Max((int)(_targetFrametime - loopTime), 0));
                _loopTime = DateTime.Now;
            }
        }
    }
}
