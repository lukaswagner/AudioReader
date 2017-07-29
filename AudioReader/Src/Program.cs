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
        private static bool _dataValid = false;
        private static Visualization _vis;

        static void Main(string[] args)
        {
            IniParser.Load();

            Dictionary<string, string> audioConfig = IniParser.GetSectionParameter("audio");

            if (!audioConfig.TryGetValue("device", out string deviceId))
            {
                for (int i = 0; i < BassWasapi.BASS_WASAPI_GetDeviceCount(); i++)
                {
                    var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
                    if (device.IsEnabled && device.IsLoopback)
                    {
                        Console.WriteLine(string.Format("{0} - {1}", i, device.name));
                    }
                }

                Console.WriteLine("Enter target device id");
                deviceId = Console.ReadLine();
            }

            int deviceId_int = Int32.Parse(deviceId);

            _data = new float[2048];

            BASS_WASAPI_DEVICEINFO devInfo = BassWasapi.BASS_WASAPI_GetDeviceInfo(deviceId_int);
            _callbackProcess = new WASAPIPROC(_callbackFunction);

            _checkError(Bass.BASS_Init(0, devInfo.mixfreq, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero));
            _say("Bass_Init done");
            _checkError(BassWasapi.BASS_WASAPI_Init(deviceId_int, devInfo.mixfreq, devInfo.mixchans, BASSWASAPIInit.BASS_WASAPI_BUFFER, 0f, 0f, _callbackProcess, IntPtr.Zero));
            _say("BASS_WASAPI_Init done");
            _checkError(BassWasapi.BASS_WASAPI_Start());
            _say("BASS_WASAPI_Start done");

            _vis = new Visualization(_data);
            _vis.Run();

            Thread.Sleep(-1);
        }

        private static void _checkError(bool success)
        {
            if (!success)
            {
                Console.Write(Bass.BASS_ErrorGetCode());
                Thread.Sleep(-1);
            }
        }

        private static void _say(object o)
        {
            Console.WriteLine(o);
        }

        private static int _callbackFunction(IntPtr buffer, int length, IntPtr user)
        {
            _dataValid = BassWasapi.BASS_WASAPI_GetData(_data, (int)(BASSData.BASS_DATA_FFT2048) | (int)(BASSData.BASS_DATA_FFT_INDIVIDUAL)) >= 0;
            return length;
        }
    }
}
