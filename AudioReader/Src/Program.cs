﻿using System;
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

            if (!Config.Get("audio/arraysize", out string arraySize))
                arraySize = "2048";
            _data = new float[Int32.Parse(arraySize)];
            if (!Config.Get("audio/reduced_arraysize", out string reducedArraySize))
                arraySize = "128";
            _reducedData = new float[Int32.Parse(reducedArraySize)];

            _checkEnabled("audio", "AudioReader", () =>
            {
                if (!Config.Get("audio/device", out string deviceId))
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

            // _vis.Run() stops further execution until vis window is closed - call last
            _checkEnabled("glsl", "GLSL renderer", () =>
            {
                _vis = new GlslRenderer(_reducedData);
                _vis.Run(60, 60);
            });
        }

        private static void _checkEnabled(string xmlName, string logName, Action enabledCallback)
        {
            bool enabled = Config.Get(xmlName + "/enabled", out string enabledOut) && bool.Parse(enabledOut);
            Log.Info("Main", logName + (enabled ? " is enabled." : " is disabled."));
            if (enabled)
                enabledCallback.Invoke();
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

            float[] reducedData = new float[_reducedData.Length];
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
