using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioReader
{
    class ArtNetOutput
    {
        private static DateTime _lastTime = DateTime.Now; 
        private static Thread _artNetOutputLoopThread = new Thread(_artNetOutputLoop);
        private static bool _run = false;

        public static void Enable()
        {
            _run = true;
            _artNetOutputLoopThread.Start();
        }

        private static void _artNetOutputLoop()
        {
            while (_run)
            {
                if((DateTime.Now - _lastTime).Milliseconds > 16)
                {
                    //Log.Debug("ArtNet", "bla");

                }
            }
        }
    }
}
