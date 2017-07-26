using System;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using Un4seen.BassWasapi;

namespace AudioReader
{
    class Program
    {
        private static WASAPIPROC _callbackProcess;
        private static float[] _data;
        private static bool _dataValid = false;
        private static Visualization _vis;
        private static BPMCounter _bpm;
        //private static System.Timers.Timer _timer;
        private static Timer _timer;
        private static int _stream;

        static void Main(string[] args)
        {
            //for (int i = 0; i < BassWasapi.BASS_WASAPI_GetDeviceCount(); i++)
            //{
            //    var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
            //    if (device.IsEnabled && device.IsLoopback)
            //    {
            //        Console.WriteLine(string.Format("{0} - {1}", i, device.name));
            //    }
            //}

            //Console.WriteLine("Enter target device id");
            //String id = Console.ReadLine();
            //int id_i = Int32.Parse(id);
            int id_i = 7;

            _data = new float[2048];

            //Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, true);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);
            BASS_WASAPI_DEVICEINFO devInfo = BassWasapi.BASS_WASAPI_GetDeviceInfo(id_i);
            _callbackProcess = new WASAPIPROC(_callbackFunction);

            _checkError(Bass.BASS_Init(0, devInfo.mixfreq, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero));
            _say("Bass_Init done");
            _checkError(BassWasapi.BASS_WASAPI_Init(id_i, devInfo.mixfreq, devInfo.mixchans, BASSWASAPIInit.BASS_WASAPI_BUFFER, 0f, 0f, _callbackProcess, IntPtr.Zero));
            _say("BASS_WASAPI_Init done");
            //BASS_WASAPI_INFO info =  BassWasapi.BASS_WASAPI_GetInfo();
            //_say("BASS_WASAPI_GetInfo done");
            //_stream = Bass.BASS_StreamCreatePush(devInfo.mixfreq, devInfo.mixchans, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, IntPtr.Zero);
            //_say("BASS_StreamCreatePush done");
            _checkError(BassWasapi.BASS_WASAPI_Start());
            _say("BASS_WASAPI_Start done");

            //BassWasapiHandler _wasapi = new BassWasapiHandler(7, false, devInfo.mixfreq, devInfo.mixchans, 0f, 0f);
            //_wasapi.Init();
            //_wasapi.SetFullDuplex(0, BASSFlag.BASS_STREAM_DECODE, false);
            //int recordStream = _wasapi.InputChannel;
            //_wasapi.Start();

            _vis = new Visualization(_data);
            _vis.Run();

            //_bpm = new BPMCounter(25, devInfo.mixfreq);
            //_bpm.BPMHistorySize = 2;
            //_bpm.MaxBPM = 250;


            //int beatCount = 0;
            //_timer = new Timer((stateInfo) =>
            //{
            //    //_vis.Beat();
            //    bool b = _bpm.ProcessAudio(_stream, true);
            //    if (b)
            //        Console.WriteLine(beatCount++);
            //    //_vis.Beat();
            //    //Console.WriteLine(_bpm.BPM);
            //}, null, 0, 25);

            //Thread t = new Thread(CreateTimer);
            //t.Start();

            //_vis = new Visualization(_data);
            //_vis.Run();

            Thread.Sleep(-1);
            //while (true) { }
        }

        //public static void CreateTimer()
        //{
        //    _timer = new System.Timers.Timer();
        //    _timer.Elapsed += _vis.Beat;
        //    _timer.Interval = 4;
        //    _timer.Enabled = true;
        //}

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
            //Bass.BASS_StreamPutData(_stream, buffer, length);
            return length;
        }
    }
}
