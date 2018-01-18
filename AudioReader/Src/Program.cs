using System;
using System.Collections.Generic;
using System.Threading;
using Un4seen.Bass;
using Un4seen.BassWasapi;

namespace AudioReader
{
    class Program
    {
        private static WASAPIPROC _callbackProcess;
        private static float[] _data;
        private static float[] _reducedData;
        private static bool _dataValid = false;
        private static GlslRenderer _vis;
        private static HueController _hueController;

        static void Main(string[] args)
        {
            Log.Enable(Log.LogLevel.Verbose);

            _data = new float[2048];
            _reducedData = new float[128];

            if (!Config.Get("audio/device", out string deviceId))
                deviceId = _listDevices();

            while(!_setUpAudio(deviceId))
                deviceId = _listDevices();

            BeatDetection.Enable(_data);
            _hueController = new HueController();
            _vis = new GlslRenderer(_reducedData);
            _vis.Run(60, 60);

            //_hueController.TurnAllTheLightsOff();
        }

        private static string _listDevices()
        {
            Log.Info("BASS Setup", "Available devices:");

            for (int i = 0; i < BassWasapi.BASS_WASAPI_GetDeviceCount(); i++)
            {
                var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
                if (device.IsEnabled && device.IsLoopback)
                {
                    Log.Info("BASS Setup", string.Format("{0} - {1}", i, device.name));
                }
            }

            Log.Info("BASS Setup", "Enter target device id:");
            return Console.ReadLine();
        }

        private static bool _setUpAudio(string deviceId)
        {
            int deviceId_int = Int32.Parse(deviceId);

            BASS_WASAPI_DEVICEINFO devInfo = BassWasapi.BASS_WASAPI_GetDeviceInfo(deviceId_int);
            _callbackProcess = new WASAPIPROC(_callbackFunction);

            return _checkError(Bass.BASS_Init(0, devInfo.mixfreq, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero), "BASS_Init")
                && _checkError(BassWasapi.BASS_WASAPI_Init(deviceId_int, devInfo.mixfreq, devInfo.mixchans, BASSWASAPIInit.BASS_WASAPI_BUFFER, 0f, 0f, _callbackProcess, IntPtr.Zero), "BASS_WASAPI_Init")
                && _checkError(BassWasapi.BASS_WASAPI_Start(), "BASS_WASAPI_Start");
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

            float[] reducedData = new float[128];
            int valuesPerReducedValue = _data.Length / reducedData.Length;
            for(int i = 0; i < _data.Length; i++)
            {
                bool isLeft = i % 2 == 0;
                int reducedIndex = i / 2 / valuesPerReducedValue;
                reducedData[isLeft ? (reducedData.Length / 2 - 1) - reducedIndex : (reducedData.Length / 2) + reducedIndex] += _data[i] / valuesPerReducedValue;
            }
            reducedData.CopyTo(_reducedData, 0);

            return length;
        }
    }
}
