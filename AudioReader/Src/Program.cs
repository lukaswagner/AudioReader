using System;
using System.Globalization;
using System.Threading;
using Un4seen.Bass;
using Un4seen.BassWasapi;

namespace AudioReader
{
    internal static class Program
    {
        private static WASAPIPROC _callbackProcess;
        private static float[] _data;
        private static float[] _reducedData;
        private static bool _dataValid = false;
        private static HueController _hueController;
        public static ManualResetEvent RendererSetUp = new ManualResetEvent(false);

        private static void Main()
        {
            Log.Enable(Config.GetDefault("log/level", "Info"));

            _data = new float[Config.GetDefault("audio/arraysize", 2048)];
            _reducedData = new float[Config.GetDefault("audio/reduced_arraysize", 128)];

            _checkEnabled("audio", "AudioReader", () =>
            {
                if (!Config.Get<string>("audio/device", out var deviceId))
                    deviceId = _listDevices();

                while (!_setUpAudio(deviceId))
                    deviceId = _listDevices();
            });

            _checkEnabled("beatdetection", "BeatDetection", () => BeatDetection.Enable(_data));

            _checkEnabled("philips_hue", "Hue output", () =>
            {
                _hueController = new HueController();
                _hueController.TurnAllTheLightsOff();
            });

            _checkEnabled("spotify", "Spotify integration", () => Spotify.Setup());
            
            _checkEnabled("glsl", "GLSL renderer", () => GlslRenderer.Enable(_reducedData));

            RendererSetUp.WaitOne();
        }

        private static void _checkEnabled(string xmlName, string logName, Action enabledCallback)
        {
            var enabled = Config.GetDefault(xmlName + "/enabled", false);
            Log.Info("Main", logName + (enabled ? " is enabled." : " is disabled."));
            if (enabled)
                enabledCallback.Invoke();
        }

        private static string _listDevices()
        {
            Log.Info("BASS Setup", "Available devices:");

            for (var i = 0; i < BassWasapi.BASS_WASAPI_GetDeviceCount(); i++)
            {
                var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
                var listInputs = Config.GetDefault("audio/list_inputs", false);
                if (device.IsEnabled && (device.IsLoopback || listInputs && device.IsInput))
                {
                    Log.Info("BASS Setup", string.Format(CultureInfo.InvariantCulture, "{0} - {1}", i, device.name));
                }
            }

            Log.Info("BASS Setup", "Enter target device id:");
            return Console.ReadLine();
        }

        private static bool _setUpAudio(string deviceId)
        {
            var deviceId_int = int.Parse(deviceId, CultureInfo.InvariantCulture);

            var devInfo = BassWasapi.BASS_WASAPI_GetDeviceInfo(deviceId_int);
            if (devInfo == null)
            {
                Log.Warn("BASS Setup", "Device " + deviceId + " does not exist.");
                return false;
            }

            _callbackProcess = new WASAPIPROC(_callbackFunction);

            if (!_checkError(Bass.BASS_Init(0, devInfo.mixfreq, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero), "BASS_Init"))
            {
                _callbackProcess = null;
            }

            if (!_checkError(BassWasapi.BASS_WASAPI_Init(deviceId_int, devInfo.mixfreq, devInfo.mixchans, BASSWASAPIInit.BASS_WASAPI_BUFFER, 0f, 0f, _callbackProcess, IntPtr.Zero), "BASS_WASAPI_Init"))
            {
                Bass.BASS_Free();
                _callbackProcess = null;
                return false;
            }

            if (!_checkError(BassWasapi.BASS_WASAPI_Start(), "BASS_WASAPI_Start"))
            {
                BassWasapi.BASS_WASAPI_Free();
                Bass.BASS_Free();
                _callbackProcess = null;
                return false;
            }

            return true;
        }

        private static bool _checkError(bool success, string step)
        {
            if (success)
                Log.Info("BASS Setup", step + " successful.");
            else
                Log.Error("BASS Setup", step + " unsuccessful. Error: " + Bass.BASS_ErrorGetCode());
            return success;
        }

        private static int _callbackFunction(IntPtr buffer, int length, IntPtr user)
        {
            _dataValid = BassWasapi.BASS_WASAPI_GetData(_data, (int)(BASSData.BASS_DATA_FFT2048) | (int)(BASSData.BASS_DATA_FFT_INDIVIDUAL)) >= 0;

            if (_dataValid)
            {
                var reducedData = new float[_reducedData.Length];
                var valuesPerReducedValue = _data.Length / reducedData.Length;
                for (var i = 0; i < _data.Length; i++)
                {
                    var isLeft = i % 2 == 0;
                    var reducedIndex = i / 2 / valuesPerReducedValue;
                    reducedData[isLeft ? (reducedData.Length / 2 - 1) - reducedIndex : (reducedData.Length / 2) + reducedIndex] += _data[i] / valuesPerReducedValue;
                }
                reducedData.CopyTo(_reducedData, 0);
            }

            return length;
        }
    }
}
